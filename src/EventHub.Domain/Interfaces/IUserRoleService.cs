namespace EventHub.Domain.Interfaces;

/// <summary>
/// Role changes performed by an administrator on another user.
/// Deliberately no role parameter: Organizer is the only role that can be
/// granted through the API. Administrator is assigned via SQL only,
/// Participant is assigned at registration.
/// </summary>
public interface IUserRoleService
{
    /// <exception cref="Exceptions.NotFoundException">User does not exist.</exception>
    Task GrantOrganizerAsync(Guid userId);

    /// <exception cref="Exceptions.NotFoundException">User does not exist.</exception>
    Task RevokeOrganizerAsync(Guid userId);
}