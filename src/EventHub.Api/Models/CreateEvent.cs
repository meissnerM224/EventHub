using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public record CreateEvent
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(200)]
    public required string Title { get; init; }
    [Required(AllowEmptyStrings = false)]  
    [MaxLength(500)]
    public required string Description { get; init; }
    [Required(AllowEmptyStrings = false)] 
    public required string Location { get; init; }
    public required DateTimeOffset StartAt { get; init; }
    public required DateTimeOffset DoorsOpenAt { get; init; }
    [Range(1, int.MaxValue)] 
    public required int MaxParticipants { get; init; }
    public required Guid OrganizerId { get; init; }
    [Range(1, int.MaxValue)]
    public required int CategoryId { get; init; }
}