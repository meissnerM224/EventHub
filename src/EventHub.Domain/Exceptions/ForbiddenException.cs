namespace EventHub.Domain.Exceptions;

public class ForbiddenException(string code, string message) : DomainException(code, message);