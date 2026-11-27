using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;

namespace EventHub.Api.Tests.Fakes;

public sealed class RecordingNotificationSender : INotificationSender
{
    private readonly List<BookingConfirmationMessage> _sent = [];
    private readonly Dictionary<Guid, int> _failuresLeft = [];
    private readonly Dictionary<Guid, int> _calls = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<BookingConfirmationMessage> Sent
    {
        get
        {
            lock (_gate) return _sent.ToList();
        }
    }

    // Keyed by user, not booking: the worker may pick a message up right after the booking,
    // so the failure has to be set up before the booking id is known.
    public void FailFor(Guid userId, int times = int.MaxValue)
    {
        lock (_gate) _failuresLeft[userId] = times;
    }

    public int CallsFor(Guid userId)
    {
        lock (_gate) return _calls.GetValueOrDefault(userId);
    }

    public Task SendBookingConfirmationAsync(BookingConfirmationMessage message, CancellationToken ct)
    {
        lock (_gate)
        {
            _calls[message.UserId] = _calls.GetValueOrDefault(message.UserId) + 1;
            if (_failuresLeft.TryGetValue(message.UserId, out var left) && left > 0)
            {
                _failuresLeft[message.UserId] = left - 1;
                throw new InvalidOperationException($"Mail server unavailable for {message.UserId}");
            }

            _sent.Add(message);
        }

        return Task.CompletedTask;
    }
}
