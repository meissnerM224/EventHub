// One boundary for the whole booking list: the start of the event.
// Before: upcoming, can be cancelled. From then on: history, read-only.
// Same rule as the backend (CancelAsync throws event.already_started).
export function hasStarted(startsAt: string, now = Date.now()): boolean {
  return new Date(startsAt).getTime() <= now;
}

// No end time in the backend yet – assume an event lasts this long.
export const ASSUMED_DURATION_MS = 3 * 60 * 60 * 1000;

// Past = started more than ASSUMED_DURATION_MS ago.
export function isPast(startsAt: string, now = Date.now()): boolean {
  return new Date(startsAt).getTime() + ASSUMED_DURATION_MS <= now;
}
