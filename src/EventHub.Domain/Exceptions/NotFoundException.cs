namespace EventHub.Domain.Exceptions;

public class NotFoundException(string code, string resource, object id)
    : DomainException(code, $"{resource} '{id}' was not found.")
{
    public string Resource { get; } = resource;
    public object Id { get; } = id;
}