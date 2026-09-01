using EventHub.Domain.Models;
using EventHub.Domain.Entities;

namespace EventHub.Domain.Interfaces;

public interface IEventsRepository
{
    Task<List<EventSummary>> GetAllAsync();
    Task<EventDetail?> GetEventByIdAsync(Guid eventId);

    Task<User?> GetOrganizerById(Guid organizerId);

    Task<Category?> GetCategoryById(int categoryId);

    Task CreateNewEvent(Event newEvent);

    Task<bool> EventExistsAsync(string title, string location, DateTimeOffset startsAt, Guid? excludeId);
     Task<Event?> GetEventEntityByIdAsync(Guid eventId);

    Task SaveChangesAsync();
}