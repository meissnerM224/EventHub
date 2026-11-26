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

export function errorMessageId(error: unknown): MessageId {
  if (error instanceof ApiError && error.code) {
    return codeMessages[error.code] ?? "common.error";
  }
  return "common.error";
}
