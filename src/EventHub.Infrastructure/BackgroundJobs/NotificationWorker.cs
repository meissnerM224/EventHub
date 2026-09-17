using EventHub.Domain.Interfaces;
using EventHub.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.BackgroundJobs;

public sealed class NotificationWorker(
    ChannelNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification worker started");

        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = message.TraceId
            });

            try
            {
                await using var serviceScope = scopeFactory.CreateAsyncScope();
                var sender = serviceScope.ServiceProvider.GetRequiredService<INotificationSender>();

                await sender.SendBookingConfirmationAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to send confirmation for booking {BookingId}", message.BookingId);
            }
        }

        logger.LogInformation("Notification worker stopped");
    }
}