namespace EventHub.Domain.Exceptions;

public class FullyBookedException(string message) : Exception(message);
