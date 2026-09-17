using EventHub.Domain.Messaging;

namespace EventHub.Domain.Interfaces;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(BookingConfirmationMessage message, CancellationToken ct = default);
}