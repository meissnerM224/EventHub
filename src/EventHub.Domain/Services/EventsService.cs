using EventHub.Domain.Entities;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;

namespace EventHub.Domain.Services;

public class EventsService(IEventsRepository repository, ICacheService cache) : IEventsService
{
    private const string ListPrefix = "events:list:";

    private static string ListKey(EventFilter f) =>
        $"{ListPrefix}:c={f.CategoryId}:l={f.Location?.ToLowerInvariant()}:f={f.From:o}.t={f.To:o:o}";

    private static string DetailKey(Guid id) => $"events:details:{id}";

    public async Task<List<EventSummary>> GetAllEventsAsync(EventFilter filter)
    {
        var normalized = new EventFilter
        {
            CategoryId = filter.CategoryId,
            Location = filter.Location?.Trim(),
            From = filter.From?.ToUniversalTime(),
            To = filter.To?.ToUniversalTime()
        };
        if (normalized.From > normalized.To) throw new BusinessRuleException("'from' must bot be after 'to'.");
        var key = ListKey(normalized);
        var cached = await cache.GetAsync<List<EventSummary>>(key);
        if (cached is not null) return cached;
        var events = await repository.GetAllAsync(normalized);
        await cache.SetAsync(key, events.ToList(), TimeSpan.FromMinutes(5));
        return events;
    }

    public async Task<EventDetail> GetEventByIdAsync(Guid eventId)
    {
        var cached = await cache.GetAsync<EventDetail>(DetailKey(eventId));
        if (cached is not null) return cached;
        var details = await repository.GetEventByIdAsync(eventId) ?? throw new NotFoundException("Event", eventId);
        await cache.SetAsync(DetailKey(eventId), details, TimeSpan.FromMinutes(5));
        return details;
    }


    public async Task<EventSummary> CreateEventAsync(
        string title,
        string description,
        string location,
        DateTimeOffset startAt,
        DateTimeOffset doorsOpenAt,
        int maxParticipants,
        Guid organizerId,
        int categoryId
    )
    {
        startAt = startAt.ToUniversalTime();
        doorsOpenAt = doorsOpenAt.ToUniversalTime();
        var existAlready = await repository.EventExistsAsync(title, location, startAt, null);
        if (existAlready) throw new AlreadyExistException($"Event {title} at {location} already exists");

        if (!await repository.OrganizerExistsAsync(organizerId))
        {
            throw new NotFoundException("Organizer", organizerId);
        }

        var category = await repository.GetCategoryById(categoryId);
        if (category is null)
        {
            throw new NotFoundException("Category", categoryId);
        }

        if (doorsOpenAt > startAt)
        {
            throw new BusinessRuleException("DoorsOpenAt cant not before StartAt.");
        }

        if (startAt <= DateTimeOffset.UtcNow)
        {
            throw new BusinessRuleException("StartAt must be in the future.");
        }

        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Location = location,
            StartsAt = startAt,
            DoorsOpenAt = doorsOpenAt,
            MaxParticipants = maxParticipants,
            OrganizerId = organizerId,
            CategoryId = categoryId,
        };
        await repository.CreateNewEvent(newEvent);
        await cache.RemoveByPrefixAsync(ListPrefix);
        return new EventSummary
        {
            Id = newEvent.Id,
            Title = newEvent.Title,
            Location = newEvent.Location,
            StartsAt = newEvent.StartsAt,
            AvailableSpots = newEvent.MaxParticipants,
            CategoryName = category.Name,
        };
    }


    public async Task<EventDetail> UpdateEventAsync(
        Guid eventId,
        string title,
        string description,
        string location,
        DateTimeOffset startAt,
        DateTimeOffset doorsOpenAt,
        int maxParticipants,
        int categoryId,
        Guid currentUserId
    )
    {
        startAt = startAt.ToUniversalTime();
        doorsOpenAt = doorsOpenAt.ToUniversalTime();
        var existing = await repository.GetEventEntityByIdAsync(eventId);
        if (existing is null) throw new NotFoundException("Event", eventId);
        if (await repository.EventExistsAsync(title, location, startAt, eventId))
        {
            throw new AlreadyExistException($"Event {title} at {location} already exists");
        }

        if (existing.OrganizerId != currentUserId) throw new ForbiddenException("Permission denied");
        var category = await repository.GetCategoryById(categoryId);
        if (category is null) throw new NotFoundException("Category", categoryId);
        if (doorsOpenAt > startAt) throw new BusinessRuleException("DoorsOpenAt can't by before StartAt.");
        if (startAt <= DateTimeOffset.UtcNow) throw new BusinessRuleException("StartAt must be in the future.");


        existing.Title = title;
        existing.Description = description;
        existing.Location = location;
        existing.StartsAt = startAt;
        existing.DoorsOpenAt = doorsOpenAt;
        existing.MaxParticipants = maxParticipants;
        existing.CategoryId = categoryId;


        await repository.SaveChangesAsync();
        await cache.RemoveByPrefixAsync(ListPrefix);
        await cache.RemoveAsync(DetailKey(eventId));
        return await repository.GetEventByIdAsync(eventId) ?? throw new NotFoundException("Event", eventId);
    }

    public async Task CancelEventAsync(Guid eventId, Guid currentUser)
    {
        var existing = await repository.GetEventEntityByIdAsync(eventId);
        if (existing is null) throw new NotFoundException("Event", eventId);
        if (existing.OrganizerId != currentUser) throw new ForbiddenException("Permission denied");
        if (existing.IsCancelled) return;
        existing.IsCancelled = true;
        existing.CancelledAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync();

        await cache.RemoveByPrefixAsync(ListPrefix);
        await cache.RemoveAsync(DetailKey(eventId));
    }
}