namespace EventHub.Domain.Exceptions;

public class NotFoundException(string resource, object id)
    : Exception($"{resource} with ID: {id} didn't exist.")
{
    public string Resource { get; } = resource;
    public object Id { get; } = id;
}