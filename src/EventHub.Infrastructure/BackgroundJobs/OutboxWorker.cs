using System.Text.Json;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.BackgroundJobs;

public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox worker started");
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.PollIntervalMs));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Outbox batch failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        logger.LogInformation("Outbox worker stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        List<OutboxMessage> claimed;

        await using (var transaction = await context.Database.BeginTransactionAsync(ct))
        {
            claimed = await context.Set<OutboxMessage>()
                .FromSql($"""
                          SELECT * FROM "OutboxMessages"
                          WHERE "ProcessedAt" IS NULL AND "Attempts" < {_options.MaxAttempts}
                          ORDER BY "OccurredAt"
                          LIMIT {_options.BatchSize}
                          FOR UPDATE SKIP LOCKED
                          """)
                .ToListAsync(ct);

            if (claimed.Count == 0) return;

            foreach (var message in claimed)
                message.Attempts++;

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        foreach (var message in claimed)
        {
            using var logScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = message.TraceId
            });

            try
            {
                var payload = JsonSerializer.Deserialize<BookingConfirmationMessage>(message.Payload)
                              ?? throw new InvalidOperationException($"Outbox payload {message.Id} is empty.");

                await sender.SendBookingConfirmationAsync(payload, ct);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.LastError = ex.Message;
                logger.LogError(ex,
                    "Outbox message {OutboxId} failed on attempt {Attempts}",
                    message.Id, message.Attempts);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}