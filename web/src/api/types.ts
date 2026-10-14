import { z } from "zod";

export const eventSummarySchema = z.object({
  id: z.string(),
  title: z.string(),
  location: z.string(),
  categoryName: z.string(),
  startsAt: z.string(),
  availableSpots: z.number(),
  imageUrl: z.string().nullable(),
});

export const eventDetailSchema = z.object({
  eventId: z.string(),
  eventTitle: z.string(),
  eventImageUrl: z.string().nullish(),
  eventDescription: z.string(),
  eventLocation: z.string(),
  eventStartsAt: z.string(),
  doorsOpenAt: z.string(),
  maxParticipants: z.number().int(),
  availableSpots: z.number().int(),
  categoryId: z.number().int(),
  categoryName: z.string(),
  categoryDescription: z.string().nullable(),
  organizerId: z.string(),
  organizerName: z.string(),
  isCancelled: z.boolean(),
});

export type EventSummary = z.infer<typeof eventSummarySchema>;
export type EventDetail = z.infer<typeof eventDetailSchema>;
