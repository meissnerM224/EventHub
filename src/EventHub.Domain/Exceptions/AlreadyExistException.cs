namespace EventHub.Domain.Exceptions;

public class AlreadyExistException(string code, string message) : DomainException(code, message);
