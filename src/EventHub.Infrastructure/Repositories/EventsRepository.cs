using EventHub.Domain.Models;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class EventsRepository(EventHubDbContext dbContext) : IEventsRepository
{


    public async Task<List<EventSummary>> GetAllAsync()
    {
        return await dbContext
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
                                 dbContext.Set<Booking>()
                                     .Count(b => b.EventId == e.Id && b.Status == BookingStatus.Confirmed),
            }).ToListAsync();
    }

    public async Task<EventDetail?> GetEventByIdAsync(Guid eventId)
    {
    return await    (
            from e in dbContext.Set<Event>()
            join u in dbContext.Users on e.OrganizerId equals u.Id
            where e.Id == eventId
            select new EventDetail
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
                OrganizerName = u.DisplayName,
                MaxParticipants = e.MaxParticipants,
                AvailableSpots = e.MaxParticipants - dbContext.Set<Booking>()
                    .Count(b => b.EventId == e.Id && b.Status == BookingStatus.Confirmed),
                IsCancelled = e.IsCancelled,
            }).FirstOrDefaultAsync();
    }

    public Task<bool> OrganizerExistsAsync(Guid organizerId) =>
        dbContext.Users.AnyAsync(u => u.Id == organizerId);

    public async Task<Category?> GetCategoryById(int categoryId)
    {
        return await dbContext.Set<Category>().FindAsync(categoryId);
    }


    public async Task CreateNewEvent(Event newEvent)
    {
        dbContext.Set<Event>().Add(newEvent);
        await dbContext.SaveChangesAsync();
    }




    public async Task<bool> EventExistsAsync(
        string title,
        string location,
        DateTimeOffset startsAt,
        Guid? excludeId = null
    )
    {
        return await dbContext.Set<Event>().AnyAsync(e =>
            e.Title == title &&
            e.Location == location &&
            e.StartsAt == startsAt &&
            (excludeId == null || e.Id != excludeId)
        );
    }

    public async Task<Event?> GetEventEntityByIdAsync(Guid eventId)
    {
        return await dbContext.Set<Event>().FindAsync(eventId);
    }


    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}