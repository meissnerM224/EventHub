namespace EventHub.Domain.Exceptions;

public class NotFoundException(string code, string resource, object id)
    : DomainException(code, $"{resource} with ID: {id} didn't exist.")
{
    public string Resource { get; } = resource;
    public object Id { get; } = id;
}