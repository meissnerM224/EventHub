namespace EventHub.Domain.Exceptions;

public class UnAuthorizedException(string message) : Exception(message)
{
}