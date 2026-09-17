using EventHub.Domain.Models;

namespace EventHub.Domain.Messaging;

public sealed record BookingConfirmationMessage(
    Guid BookingId,
    Guid UserId,
    BookingConfirmation Confirmation,
    string? TraceId);