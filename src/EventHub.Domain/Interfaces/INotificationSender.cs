using EventHub.Domain.Messaging;

namespace EventHub.Domain.Interfaces;

public interface INotificationSender
{
    Task SendBookingConfirmationAsync(BookingConfirmationMessage message, CancellationToken ct);
}