using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public class LoginUser
{
    [Required(AllowEmptyStrings = false), EmailAddress]
    public required string Email { get; init; }

    [Required(AllowEmptyStrings = false)] public required string Password { get; init; }
}
