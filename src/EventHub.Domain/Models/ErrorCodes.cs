namespace EventHub.Domain.Models;

public static class ErrorCodes
{
    public const string AuthInvalidCredentials = "auth.invalid_credentials";
    public const string BookingAlreadyExists = "booking.already_exists";
    public const string BookingNotFound = "booking.not_found";
    public const string CategoryAlreadyExists = "category.already_exists";
    public const string CategoryNotFound = "category.not_found";
    public const string EventAlreadyExists = "event.already_exists";
    public const string EventAlreadyStarted = "event.already_started";
    public const string EventCancelled = "event.cancelled";
    public const string EventCapacityTooLow = "event.capacity_too_low";
    public const string EventFullyBooked = "event.fully_booked";
    public const string EventInvalidDates = "event.invalid_dates";
    public const string EventInvalidImage = "event.invalid_image";
    public const string EventNotFound = "event.not_found";
    public const string EventNotOwner = "event.not_owner";
    public const string FileMissing = "file.missing";
    public const string FileUnsupported = "file.unsupported";
    public const string UserEmailTaken = "user.email_taken";
    public const string UserNotFound = "user.not_found";
    public const string UserRegistrationFailed = "user.registration_failed";
}