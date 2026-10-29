import { de } from "./messages/de";
import { en } from "./messages/en";

export const messages = { de, en };
export type Locale = keyof typeof messages;

export function detectLocale(): Locale {
  const preferred = navigator.languages.find(
    (l) => l.startsWith("de") || l.startsWith("en"),
  );
  return preferred?.startsWith("de") ? "de" : "en";
}
