const formatters = new Map<string, Intl.DateTimeFormat>();

// TODO(me): add timezone option for use dynamic location instead of hardcoded german timezone
export function formatDate(isoString: string, locale: string): string {
  let formatter = formatters.get(locale);
  if (!formatter) {
    formatter = new Intl.DateTimeFormat(locale, {
      weekday: "short",
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      timeZone: "Europe/Berlin",
    });
    formatters.set(locale, formatter);
  }
  return formatter.format(new Date(isoString));
}
