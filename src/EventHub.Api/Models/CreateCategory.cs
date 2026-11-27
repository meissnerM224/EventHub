using System.ComponentModel.DataAnnotations;
namespace EventHub.Api.Models;

public class CreateCategory
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(400)]
    public string Description { get; init; } = string.Empty;
}