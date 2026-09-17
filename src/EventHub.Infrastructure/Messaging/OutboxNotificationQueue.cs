using System.Text.Json;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;

namespace EventHub.Infrastructure.Messaging;

public class OutboxNotificationQueue(EventHubDbContext context) : INotificationQueue
{
    public async ValueTask EnqueueAsync(
        BookingConfirmationMessage message, CancellationToken ct = default)
    {
        context.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(BookingConfirmationMessage),
            Payload = JsonSerializer.Serialize(message),
            TraceId = message.TraceId,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync(ct);
    }
}