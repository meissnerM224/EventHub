namespace EventHub.Domain.Models;

public class CategorySummary
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}