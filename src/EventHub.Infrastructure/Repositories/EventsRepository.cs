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
        return await _dbContext
            .Set<Event>()
            .Where(e => !e.IsCancelled)
            .Select(e => new EventSummary
            {
                Id = e.Id,
                Title = e.Title,
                StartsAt = e.StartsAt,
                Location = e.Location,
                CategoryName = e.Category.Name,
                AvailableSpots = e.MaxParticipants -
                                 _dbContext.Set<Booking>()
                                     .Count(b => b.EventId == e.Id && b.Status == BookingStatus.Confirmed),
            }).ToListAsync();
    }

    public async Task<EventDetail?> GetEventByIdAsync(Guid eventId)
    {
        return await _dbContext.Set<Event>()
            .Where(e => e.Id == eventId)
            .Select(e => new EventDetail
            {
                EventId = e.Id,
                EventTitle = e.Title,
                EventDescription = e.Description,
                EventLocation = e.Location,
                EventStartsAt = e.StartsAt,
                DoorsOpenAt = e.DoorsOpenAt,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                CategoryDescription = e.Category.Description,
                OrganizerId = e.OrganizerId,
                OrganizerName = e.Organizer.Name,
                MaxParticipants = e.MaxParticipants,
                AvailableSpots = e.MaxParticipants - _dbContext.Set<Booking>()
                    .Count(b => b.EventId == e.Id && b.Status == BookingStatus.Confirmed),
                IsCancelled = e.IsCancelled,
            }).FirstOrDefaultAsync();
    }

    public async Task<User?> GetOrganizerById(Guid organizerId)
    {
        return await _dbContext.Set<User>().FindAsync(organizerId);
    }

    public async Task<Category?> GetCategoryById(int categoryId)
    {
        return await _dbContext.Set<Category>().FindAsync(categoryId);
    }


    public async Task CreateNewEvent(Event newEvent)
    {
        _dbContext.Set<Event>().Add(newEvent);
        await _dbContext.SaveChangesAsync();
    }




    public async Task<bool> EventExistsAsync(
        string title,
        string location,
        DateTimeOffset startsAt,
        Guid? excludeId = null
    )
    {
        return await _dbContext.Set<Event>().AnyAsync(e =>
            e.Title == title &&
            e.Location == location &&
            e.StartsAt == startsAt &&
            (excludeId == null || e.Id != excludeId)
        );
    }

    public async Task<Event?> GetEventEntityByIdAsync(Guid eventId)
    {
        return await _dbContext.Set<Event>().FindAsync(eventId);
    }


    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}