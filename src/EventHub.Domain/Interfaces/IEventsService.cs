using EventHub.Domain.Models;

namespace EventHub.Domain.Interfaces;

public interface IEventsService
{
    Task<List<EventSummary>> GetAllEventsAsync(EventFilter filter);
    Task<EventDetail> GetEventByIdAsync(Guid eventId);

    Task<EventSummary> CreateEventAsync(
        string title,
        string description,
        string location,
        string? imageUrl,
        DateTimeOffset startAt,
        DateTimeOffset doorsOpenAt,
        int maxParticipants,
        Guid organizerId,
        int categoryId
    );

    Task<EventDetail> UpdateEventAsync(
        Guid eventId,
        string title,
        string description,
        string? imageUrl,
        string location,
        DateTimeOffset startAt,
        DateTimeOffset doorsOpenAt,
        int maxParticipants,
        int categoryId,
        Guid organizerId
    );

    Task CancelEventAsync(Guid eventId, Guid currentUserId);
}