import { ApiError } from "./client.ts";
import type { MessageId } from "../i18n/locale.ts";

const codeMessages: Record<string, MessageId> = {
  "event.fully_booked": "error.event.fully_booked",
  "booking.already_exists": "error.booking.already_exists",
  "auth.invalid_credentials": "error.auth.invalid_credentials",
  "booking.not_found": "error.booking.not_found",
  "category.not_found": "error.category.not_found",
  "category.already_exists": "error.category.already_exists",
  "event.already_exists": "error.event.already_exists",
  "event.already_started": "error.event.already_started",
  "event.cancelled": "error.event.cancelled",
  "event.capacity_too_low": "error.event.capacity_too_low",
  "event.invalid_dates": "error.event.invalid_dates",
  "event.invalid_image": "error.event.invalid_image",
  "event.not_found": "error.event.not_found",
  "event.not_owner": "error.not_owner",
  "file.missing": "error.file.missing",
  "file.unsupported": "error.file.unsupported",
  "user.email_taken": "error.user.email_taken",
  "user.not_found": "error.user.not_found",
  "user.registration_failed": "error.registration_failed",
};

// For errors without a domain code (e.g. 403 from [Authorize], 413 from the upload size limit,
// a 500 or a proxy error page) the status still says more than "an error occurred".
function statusMessage(status: number): MessageId {
  if (status === 401) return "error.status.unauthorized";
  if (status === 403) return "error.status.forbidden";
  if (status === 404) return "error.status.not_found";
  if (status === 413) return "error.status.too_large";
  if (status >= 500) return "error.status.server";
  return "common.error";
}

// The only way an API error reaches the UI: code first, then status, then the generic text.
// ApiError.message is never shown – it is for logs and the console.
export function errorMessageId(error: unknown): MessageId {
  if (!(error instanceof ApiError)) return "common.error";
  if (error.code && codeMessages[error.code]) return codeMessages[error.code];
  return statusMessage(error.status);
}
