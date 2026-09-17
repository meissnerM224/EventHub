namespace EventHub.Domain.Exceptions;

public class AlreadyExistException(string message) : Exception(message)
{
}