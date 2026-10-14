import { eventSummarySchema, eventDetailSchema } from "./types";
import { apiGet } from "./client";
import { z } from "zod";

const eventListSchema = z.array(eventSummarySchema);
export const getEvents = () =>
  apiGet("api/events", (data) => eventListSchema.parse(data));

export const getEventById = (id: string) =>
  apiGet(`/api/events/${id}`, (data) => eventDetailSchema.parse(data));
