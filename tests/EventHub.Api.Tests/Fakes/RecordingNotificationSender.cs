using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;

namespace EventHub.Api.Tests.Fakes;

public sealed class RecordingNotificationSender : INotificationSender
{
    private readonly List<BookingConfirmationMessage> _sent = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<BookingConfirmationMessage> Sent
    {
        get
        {
            lock (_gate) return _sent.ToList();
        }
    }

    public Task SendBookingConfirmationAsync(BookingConfirmationMessage message, CancellationToken ct)
    {
        lock (_gate) _sent.Add(message);
        return Task.CompletedTask;
    }
}