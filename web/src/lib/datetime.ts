// <input type="datetime-local"> gives "2026-12-24T19:30" – no zone, meaning
// the user's local time. new Date() reads it exactly that way, and
// toISOString() turns it into UTC for the backend.
export function localInputToUtcIso(value: string): string {
    return new Date(value).toISOString();
}