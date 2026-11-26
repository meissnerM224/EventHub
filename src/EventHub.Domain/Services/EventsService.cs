using EventHub.Domain.Entities;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using EventHub.Domain.Storage;

namespace EventHub.Domain.Services;

public class EventsService(
    IEventsRepository repository,
    IBookingRepository bookingRepository,
    ITransactionRunner transactionRunner,
    ICacheService cache) : IEventsService
{
    private static string? NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        if (!imageUrl.StartsWith(ImagePath.EventImagePrefix, StringComparison.Ordinal) || imageUrl.Contains(".."))
        {
            throw new BusinessRuleException(ErrorCodes.EventInvalidImage, "imageUrl must reference an uploaded image.");
        }

        return imageUrl;
    }

    public async Task<List<EventSummary>> GetAllEventsAsync(EventFilter filter)
    {
        var normalized = new EventFilter
        {
            CategoryId = filter.CategoryId,
            Location = filter.Location?.Trim(),
            From = filter.From?.ToUniversalTime(),
            To = filter.To?.ToUniversalTime()
        };
        if (normalized.From > normalized.To) throw new BusinessRuleException(ErrorCodes.EventInvalidDates, "'from' must not be after 'to'.");
        var key = EventCacheKeys.List(normalized);
        var cached = await cache.GetAsync<List<EventSummary>>(key);
        if (cached is not null) return cached;
        var events = await repository.GetAllAsync(normalized);
        await cache.SetAsync(key, events.ToList(), TimeSpan.FromMinutes(5));
        return events;
    }

    public async Task<EventDetail> GetEventByIdAsync(Guid eventId)
    {
        var cached = await cache.GetAsync<EventDetail>(EventCacheKeys.Detail(eventId));
        if (cached is not null) return cached;
        var details = await repository.GetEventByIdAsync(eventId) ?? throw new NotFoundException(ErrorCodes.EventNotFound, "Event", eventId);
        await cache.SetAsync(EventCacheKeys.Detail(eventId), details, TimeSpan.FromMinutes(5));
        return details;
    }


    public async Task<EventSummary> CreateEventAsync(string title,
        string description,
        string location,
        string? imageUrl,
        DateTimeOffset startAt,
        DateTimeOffset doorsOpenAt,
        int maxParticipants,
        Guid organizerId,
        int categoryId)
    {
        var startAtData = startAt.ToUniversalTime();
        var doorsOpenAtData = doorsOpenAt.ToUniversalTime();
        var existAlready = await repository.EventExistsAsync(title, location, startAtData, null);
        if (existAlready) throw new AlreadyExistException(ErrorCodes.EventAlreadyExists, $"Event {title} at {location} already exists");

        if (!await repository.OrganizerExistsAsync(organizerId))
        {
            throw new NotFoundException(ErrorCodes.UserNotFound, "Organizer", organizerId);
        }

        var category = await repository.GetCategoryById(categoryId);
        if (category is null) throw new NotFoundException(ErrorCodes.CategoryNotFound, "Category", categoryId);


        if (doorsOpenAtData > startAtData) throw new BusinessRuleException(ErrorCodes.EventInvalidDates, "DoorsOpenAt must not be after StartAt.");


        if (startAtData <= DateTimeOffset.UtcNow) throw new BusinessRuleException(ErrorCodes.EventInvalidDates, "StartAt must be in the future.");
        var normalizedImageUrl = NormalizeImageUrl(imageUrl);

        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            ImageUrl = normalizedImageUrl,
            Description = description,
            Location = location,
            StartsAt = startAtData,
            DoorsOpenAt = doorsOpenAtData,
            MaxParticipants = maxParticipants,
            OrganizerId = organizerId,
            CategoryId = categoryId,
        };
        await repository.CreateNewEvent(newEvent);
        await cache.RemoveByPrefixAsync(EventCacheKeys.ListPrefix);
        return new EventSummary
        {
            Id = newEvent.Id,
            Title = newEvent.Title,
            ImageUrl = newEvent.ImageUrl,
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
        string? imageUrl,
        string location,
        DateTimeOffset startAt,
        DateTimeOffset doorsOpenAt,
        int maxParticipants,
        int categoryId,
        Guid currentUserId
    )
    {
        var startAtData = startAt.ToUniversalTime();
        var doorsOpenAtData = doorsOpenAt.ToUniversalTime();
        // Lock the event row like BookAsync does, so no booking can slip in between counting and saving the new capacity.
        await transactionRunner.RunAsync(async () =>
        {
            var existing = await repository.GetEventEntityForUpdateAsync(eventId);
            if (existing is null) throw new NotFoundException(ErrorCodes.EventNotFound, "Event", eventId);
            if (existing.OrganizerId != currentUserId) throw new ForbiddenException(ErrorCodes.EventNotOwner, "Permission denied");
            if (existing.IsCancelled) throw new BusinessRuleException(ErrorCodes.EventCancelled, "A cancelled event can't be updated.");
            if (await repository.EventExistsAsync(title, location, startAtData, eventId))
            {
                throw new AlreadyExistException(ErrorCodes.EventAlreadyExists, $"Event {title} at {location} already exists");
            }

            var category = await repository.GetCategoryById(categoryId);
            if (category is null) throw new NotFoundException(ErrorCodes.CategoryNotFound, "Category", categoryId);
            if (doorsOpenAtData > startAtData) throw new BusinessRuleException(ErrorCodes.EventInvalidDates, "DoorsOpenAt must not be after StartAt.");
            if (startAtData <= DateTimeOffset.UtcNow) throw new BusinessRuleException(ErrorCodes.EventInvalidDates, "StartAt must be in the future.");
            var confirmed = await bookingRepository.CountConfirmedAsync(eventId);
            if (maxParticipants < confirmed)
                throw new BusinessRuleException(ErrorCodes.EventCapacityTooLow,
                    $"MaxParticipants can't be lower than the {confirmed} confirmed bookings.");

            existing.Title = title;
            existing.Description = description;
            existing.ImageUrl = NormalizeImageUrl(imageUrl);
            existing.Location = location;
            existing.StartsAt = startAtData;
            existing.DoorsOpenAt = doorsOpenAtData;
            existing.MaxParticipants = maxParticipants;
            existing.CategoryId = categoryId;

            await repository.SaveChangesAsync();
            return existing;
        });

        await cache.RemoveByPrefixAsync(EventCacheKeys.ListPrefix);
        await cache.RemoveAsync(EventCacheKeys.Detail(eventId));
        return await repository.GetEventByIdAsync(eventId) ?? throw new NotFoundException(ErrorCodes.EventNotFound, "Event", eventId);
    }

    public async Task CancelEventAsync(Guid eventId, Guid currentUser)
    {
        var existing = await repository.GetEventEntityByIdAsync(eventId);
        if (existing is null) throw new NotFoundException(ErrorCodes.EventNotFound, "Event", eventId);
        if (existing.OrganizerId != currentUser) throw new ForbiddenException(ErrorCodes.EventNotOwner, "Permission denied");
        if (existing.IsCancelled) return;
        existing.IsCancelled = true;
        existing.CancelledAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync();

        await cache.RemoveByPrefixAsync(EventCacheKeys.ListPrefix);
        await cache.RemoveAsync(EventCacheKeys.Detail(eventId));
    }

    public async Task<List<CategorySummary>> GetCategoriesAsync()
    {
        var cached = await cache.GetAsync<List<CategorySummary>>(EventCacheKeys.Categories);
        if (cached is not null) return cached;

        var categories = await repository.GetCategoriesAsync();
        await cache.SetAsync(EventCacheKeys.Categories, categories, TimeSpan.FromHours(1));
        return categories;
    }
}