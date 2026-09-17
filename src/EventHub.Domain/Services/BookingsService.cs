using System.Diagnostics;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Messaging;
using EventHub.Domain.Models;

namespace EventHub.Domain.Services;

public sealed class BookingsService(
    IBookingRepository bookingRepository,
    IEventsRepository eventsRepository,
    ITransactionRunner transactionRunner,
    INotificationQueue notificationQueue)
    : IBookingsService
{
    public async Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(Guid eventId, Guid currentUserId)
    {
        var evt = await eventsRepository.GetEventEntityByIdAsync(eventId)
                  ?? throw new NotFoundException(nameof(Event), eventId);

        if (evt.OrganizerId != currentUserId)
            throw new ForbiddenException("Only the organizer can see the participants of this event.");

        return await bookingRepository.GetConfirmedBookingsAsync(eventId);
    }

    public async Task<BookingConfirmation> BookAsync(Guid eventId, Guid userId)
    {
        var traceId = Activity.Current?.Id;
        return await transactionRunner.RunAsync(async () =>
        {
            var evt = await eventsRepository.GetEventEntityForUpdateAsync(eventId);

            if (evt == null) throw new NotFoundException($"Event {eventId} doesn't exist.", eventId);
            if (evt.IsCancelled) throw new BusinessRuleException("The Event is canceled");
            if (evt.StartsAt <= DateTimeOffset.Now)
            {
                throw new BusinessRuleException($"The Event already started at {evt.StartsAt}.");
            }

            if (await bookingRepository.HasConfirmedBookingAsync(eventId, userId))
            {
                throw new AlreadyExistException("You already signed up for this Event");
            }

            var confirmed = await bookingRepository.CountConfirmedAsync(eventId);
            if (confirmed >= evt.MaxParticipants) throw new FullyBookedException("Sorry, the Event is sold out");
            var bookingRequest = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Confirmed,
                BookedAt = DateTimeOffset.UtcNow
            };
            await bookingRepository.AddAsync(bookingRequest);

            var confirmation = new BookingConfirmation
            {
                BookingId = bookingRequest.Id,
                EventId = evt.Id,
                EventTitle = evt.Title,
                EventStartAt = evt.StartsAt,
                BookedAt = bookingRequest.BookedAt,
            };
            await notificationQueue.EnqueueAsync(
                new BookingConfirmationMessage(
                    bookingRequest.Id,
                    userId,
                    confirmation,
                    traceId),
                CancellationToken.None);
            return confirmation;
        });
    }

    public async Task CancelBookingAsync(Guid eventId, Guid userId)
    {
        var evt = await eventsRepository.GetEventEntityByIdAsync(eventId) ??
                  throw new NotFoundException(nameof(Event), eventId);

        var booking = await bookingRepository.GetConfirmedBookingByIdAsync(evt.Id, userId);
        if (booking is null)
        {
            if (await bookingRepository.HasCancelledBookingAsync(eventId, userId)) return;

            throw new NotFoundException($"No Booking found for User", new { eventId, userId });
        }


        if (evt.IsCancelled) throw new BusinessRuleException("The Event is cancelled");
        if (evt.StartsAt <= DateTimeOffset.UtcNow) throw new BusinessRuleException("Event already started.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTimeOffset.UtcNow;
        await bookingRepository.SaveAsync();
    }
}