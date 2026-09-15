using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public class RegisterUser
{
    [Required(AllowEmptyStrings = false), EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required(AllowEmptyStrings = false), MinLength(8), MaxLength(128)]
    public required string Password { get; init; }

    [Required(AllowEmptyStrings = false), MaxLength(100)]
    public required string DisplayName { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Role { get; init; }
}