namespace EventHub.Domain.Entities;

public class Category
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string Description  { get; init; } = string.Empty;
    
}