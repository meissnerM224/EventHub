using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authentication;

namespace EventHub.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string displayName, string role);
    Task<AuthResult> LoginAsync(string email, string password);
}