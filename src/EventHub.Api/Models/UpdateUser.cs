using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public class UpdateUser
{
    [EmailAddress, MaxLength(256)] public string? Email { get; init; } = null;

    [MinLength(8), MaxLength(128)] public string? Password { get; init; } = null;

    [MaxLength(100)] public string? DisplayName { get; init; } = null;

    public string? Role { get; init; } = null;
}