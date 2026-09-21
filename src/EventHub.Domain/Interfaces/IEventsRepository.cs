using EventHub.Domain.Entities;
using EventHub.Domain.Models;

namespace EventHub.Domain.Interfaces;

public interface IEventsRepository
{
    Task<List<EventSummary>> GetAllAsync(EventFilter? filter = null);
    Task<EventDetail?> GetEventByIdAsync(Guid eventId);


    Task<bool> OrganizerExistsAsync(Guid organizerId);

    Task<Category?> GetCategoryById(int categoryId);

    Task CreateNewEvent(Event newEvent);

    Task<bool> EventExistsAsync(string title, string location, DateTimeOffset startsAt, Guid? excludeId);
    Task<Event?> GetEventEntityByIdAsync(Guid eventId);

    Task SaveChangesAsync();

    Task<Event?> GetEventEntityForUpdateAsync(Guid id);
}