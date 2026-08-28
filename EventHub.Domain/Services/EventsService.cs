using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
namespace EventHub.Domain.Services;

public class EventsService(IEventsRepository repository): IEventsService
{
    public async  Task<List<EventSummary>> GetAllEventsAsync()
    {
        return await repository.GetAllAsync();
    }
}