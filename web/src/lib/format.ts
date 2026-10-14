const dateTimeFormat = new Intl.DateTimeFormat("de-DE", {
  weekday: "short",
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: "Europe/Berlin",
});

// TODO(me): add timezone option for use dynamic location instead of hardcoded german timezone
export function formatDate(isoString: string): string {
  return dateTimeFormat.format(new Date(isoString));
}
