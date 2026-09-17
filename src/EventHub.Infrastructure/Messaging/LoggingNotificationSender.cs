using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;
using EventHub.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.Messaging;

public sealed class LoggingNotificationSender(
    UserManager<AppUser> userManager,
    ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public async Task SendBookingConfirmationAsync(
        BookingConfirmationMessage message, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(message.UserId.ToString())
                   ?? throw new InvalidOperationException(
                       $"User {message.UserId} not found for booking {message.BookingId}.");

        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        logger.LogInformation(
            "Confirmation sent to {Email} for booking {BookingId} (trace {TraceId})",
            user.Email, message.BookingId, message.TraceId);
    }
}