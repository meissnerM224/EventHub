using EventHub.Domain.Models;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class EventsRepository(EventHubDbContext dbContext) : IEventsRepository
{
    private readonly DbContext _dbContext = dbContext;


    public async Task<List<EventSummary>> GetAllAsync()
    {
        return await _dbContext.Set<Event>().Select(e => new EventSummary
        {
            Id = e.Id,
            Title = e.Title,
            StarstAt = e.StartsAt,
            Location = e.Location,
            CategoryName = e.Category.Name,
            AvailableSpots = e.MaxParticipants -
                             _dbContext.Set<Booking>()
                                 .Count(b =>b.EventId == e.Id &&  b.Status == BookingStatus.Confirmed),
        }).ToListAsync();
    }
}