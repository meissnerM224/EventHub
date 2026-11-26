import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { apiFetch, apiGet } from "./client";
import { useMe } from "./auth";

export const myBookingSchema = z.object({
  bookingId: z.string(),
  eventId: z.string(),
  eventTitle: z.string(),
  eventStartsAt: z.string(),
  isCancelled: z.boolean(),
  bookedAt: z.string(),
});
export type MyBooking = z.infer<typeof myBookingSchema>;

const myBookingListSchema = z.array(myBookingSchema);

export const getMyBookings = () =>
  apiGet("/api/me/bookings", (data) => myBookingListSchema.parse(data));

export async function bookEvent(eventId: string): Promise<void> {
  await apiFetch(`/api/events/${eventId}/bookings`, { method: "POST" });
}

export async function cancelBooking(eventId: string): Promise<void> {
  await apiFetch(`/api/events/${eventId}/bookings/me`, { method: "DELETE" });
}

// --------------- Hooks --------------- \\

export const myBookingsKey = ["me", "bookings"] as const;

export function useMyBookings() {
  const { data: user } = useMe();
  return useQuery({
    queryKey: myBookingsKey,
    queryFn: getMyBookings,
    enabled: !!user, // no request without a user → no pointless 401
  });
}

// Both mutations change the spot count (events) and the user's list (me/bookings).
function useBookingMutation(fn: (eventId: string) => Promise<void>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: fn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["events"] }),
        queryClient.invalidateQueries({ queryKey: myBookingsKey }),
      ]),
  });
}

export const useBookEvent = () => useBookingMutation(bookEvent);
export const useCancelBooking = () => useBookingMutation(cancelBooking);
