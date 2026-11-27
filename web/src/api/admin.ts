import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { apiFetch, apiGet, apiPost } from "./client";
import { categorySchema } from "./types";

export const adminUserSchema = z.object({
  userId: z.string(),
  displayName: z.string(),
  email: z.string(),
  roles: z.array(z.string()),
});
export type AdminUser = z.infer<typeof adminUserSchema>;

const adminUserListSchema = z.array(adminUserSchema);

export const getUsers = () =>
  apiGet("/api/admin/users", (data) => adminUserListSchema.parse(data));

const organizerUrl = (userId: string) =>
  `/api/admin/users/${userId}/roles/organizer`;

export async function setOrganizer(userId: string, grant: boolean) {
  await apiFetch(organizerUrl(userId), { method: grant ? "PUT" : "DELETE" });
}

export type NewCategory = { name: string; description: string };

export const createCategory = (category: NewCategory) =>
  apiPost("/api/categories", category, (data) => categorySchema.parse(data));

const usersKey = ["admin", "users"] as const;

export const useAdminUsers = () =>
  useQuery({ queryKey: usersKey, queryFn: getUsers });

export function useSetOrganizer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, grant }: { userId: string; grant: boolean }) =>
      setOrganizer(userId, grant),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersKey }),
  });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createCategory,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["categories"] }),
  });
}
