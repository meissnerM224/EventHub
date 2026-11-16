using EventHub.Domain.Models;

namespace EventHub.Domain.Exceptions;

public class FullyBookedException(string message) : DomainException(ErrorCodes.EventFullyBooked, message);
