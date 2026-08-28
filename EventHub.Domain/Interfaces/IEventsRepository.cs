using EventHub.Domain.Models;
using EventHub.Domain.Entities;

namespace EventHub.Domain.Interfaces;

public interface IEventsRepository
{
    Task<List<EventSummary>> GetAllAsync();
    // Task<Event?> GetById(Guid id);
    // Task Add(Event e);
    // Task Update(Event e);
    // Task Delete(Guid eventId);
}