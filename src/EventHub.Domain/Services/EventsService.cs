using EventHub.Domain.Entities;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;

namespace EventHub.Domain.Services;

public class EventsService(IEventsRepository repository) : IEventsService
{
    public async Task<List<EventSummary>> GetAllEventsAsync()
    {
        return await repository.GetAllAsync();
    }

    public async Task<EventDetail> GetEventByIdAsync(Guid eventId)
    {
        return await repository.GetEventByIdAsync(eventId) ?? throw new NotFoundException("Event", eventId);
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
        if (existAlready) throw new AlreadyExistsException($"Event {title} at {location} already exists");

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
            throw new BusinessRuleException("DoorsOpenAt darf nicht nach StartAt liegen.");
        }

        if (startAt <= DateTimeOffset.UtcNow)
        {
            throw new BusinessRuleException("StartAt muss in der Zukunft liegen.");
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
            throw new AlreadyExistsException($"Event {title} at {location} already exists");
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
    }
}