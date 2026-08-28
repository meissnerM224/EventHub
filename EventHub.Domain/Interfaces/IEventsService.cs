using EventHub.Domain.Models;
namespace EventHub.Domain.Interfaces;

public interface IEventsService
{
    Task<List<EventSummary>> GetAllEventsAsync();
}