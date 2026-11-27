import { useRef } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { eventSummarySchema, eventDetailSchema } from "./types";
import { apiFetch, apiPost, apiGet, parseJson } from "./client";
import { z } from "zod";

const eventListSchema = z.array(eventSummarySchema);
export const getEvents = () =>
  apiGet("/api/events", (data) => eventListSchema.parse(data));

export const getEventById = (id: string) =>
  apiGet(`/api/events/${id}`, (data) => eventDetailSchema.parse(data));

export type NewEvent = {
  title: string;
  description: string;
  location: string;
  startAt: string;
  doorsOpenAt: string;
  maxParticipants: number;
  categoryId: number;
  image: string | null;
};
const uploadResultSchema = z.object({ url: z.string() });

export async function uploadImage(file: File): Promise<string> {
  const body = new FormData();
  body.append("file", file);
  const response = await apiFetch("/api/uploads/images", {
    method: "POST",
    body,
  });
  const { url } = await parseJson(response, (data) =>
    uploadResultSchema.parse(data),
  );
  return url;
}
export const createEvent = (event: NewEvent) =>
  apiPost("/api/events", event, (data) => eventSummarySchema.parse(data));

export type CreateEventInput = {
  event: Omit<NewEvent, "image">;
  imageFile: File | null;
};

export function useCreateEvent() {
  const queryClient = useQueryClient();
  // The upload happens before the create. If the create fails (e.g. invalid dates) and the
  // form is sent again with the same file, reuse the stored image instead of uploading a copy.
  // Images that are never used are removed by the ImageCleanupWorker in the backend.
  const uploaded = useRef<{ file: File; url: string } | null>(null);
  return useMutation({
    mutationFn: async ({ event, imageFile }: CreateEventInput) => {
      let image: string | null = null;
      if (imageFile) {
        if (uploaded.current?.file !== imageFile) {
          uploaded.current = {
            file: imageFile,
            url: await uploadImage(imageFile),
          };
        }
        image = uploaded.current.url;
      }
      return createEvent({ ...event, image });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["events"] }),
  });
}
