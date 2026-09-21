using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Repositories;

public class EventsRepository(EventHubDbContext dbContext) : IEventsRepository
{
    public async Task<List<EventSummary>> GetAllAsync(EventFilter? filter = null)
    {
        var query = dbContext.Events.Where(e => !e.IsCancelled);
        if (filter?.CategoryId is { } categoryId) query = query.Where(e => e.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(filter?.Location))
            query = query.Where(e => e.Location.Contains(filter.Location));
        if (!string.IsNullOrWhiteSpace(filter?.ImageUrl))
            query = query.Where(e => e.Location.Contains(filter.ImageUrl));
        if (filter?.From is { } from) query = query.Where(e => e.StartsAt >= from);
        if (filter?.To is { } to) query = query.Where(e => e.StartsAt <= to);
        return await query
            .OrderBy(e => e.StartsAt)
            .Select(e => new EventSummary
            {
                Id = e.Id,
                Title = e.Title,
                ImageUrl = e.ImageUrl,
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
        return await (
            from e in dbContext.Set<Event>()
            join u in dbContext.Users on e.OrganizerId equals u.Id
            where e.Id == eventId
            select new EventDetail
            {
                EventId = e.Id,
                EventTitle = e.Title,
                EventImageUrl = e.ImageUrl,
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

    public Task<bool> OrganizerExistsAsync(Guid organizerId) => dbContext.Users.AnyAsync(u => u.Id == organizerId);

    public async Task<Category?> GetCategoryById(int categoryId) => await dbContext.Categories.FindAsync(categoryId);


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

    public async Task<Event?> GetEventEntityByIdAsync(Guid eventId) => await dbContext.Events.FindAsync(eventId);


    public async Task SaveChangesAsync() => await dbContext.SaveChangesAsync();


    public Task<Event?> GetEventEntityForUpdateAsync(Guid id)
    {
        return dbContext.Set<Event>()
            .FromSql($"""SELECT * FROM "Events" WHERE "Id" = {id} FOR UPDATE""")
            .FirstOrDefaultAsync();
    }
}