using System.Threading.Channels;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;

namespace EventHub.Infrastructure.Messaging;

public class ChannelNotificationQueue : INotificationQueue
{
    private readonly Channel<BookingConfirmationMessage> _channel =
        Channel.CreateBounded<BookingConfirmationMessage>(
            new BoundedChannelOptions(capacity: 100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });

    public ValueTask EnqueueAsync(BookingConfirmationMessage message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<BookingConfirmationMessage> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}