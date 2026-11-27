// One boundary for the whole booking list: the start of the event.
// Before: upcoming, can be cancelled. From then on: history, read-only.
// Same rule as the backend (CancelAsync throws event.already_started).
export function hasStarted(startsAt: string, now = Date.now()): boolean {
  return new Date(startsAt).getTime() <= now;
}
