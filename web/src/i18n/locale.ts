import { de } from "./messages/de.ts";
import { en } from "./messages/en.ts";

export const messages = { de, en };
export type Locale = keyof typeof messages;
export type MessageId = keyof typeof de;

export function detectLocale(): Locale {
  const preferred = navigator.languages.find(
    (l) => l.startsWith("de") || l.startsWith("en"),
  );
  return preferred?.startsWith("de") ? "de" : "en";
}
declare global {
  namespace FormatjsIntl {
    interface Message {
      ids: MessageId;
    }
  }
}
