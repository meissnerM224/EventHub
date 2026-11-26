import { ApiError, apiFetch, apiGet, apiPost } from "./client";
import { currentUserSchema, type CurrentUser } from "./types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { resetSession } from "./queryClient";

export type Credentials = {
  email: string;
  password: string;
};

export type Registration = Credentials & {
  displayName: string;
};

const parseUser = (data: unknown) => currentUserSchema.parse(data);

export async function getMe(): Promise<CurrentUser | null> {
  try {
    return await apiGet("/api/auth/me", parseUser);
  } catch (e) {
    if (e instanceof ApiError && e.status === 401) return null;
    throw e;
  }
}

// Only needs the roles – works for CurrentUser and AdminUser alike.
export const hasRole = (
  user: { roles: string[] } | null | undefined,
  role: Role,
) => user?.roles.includes(role) ?? false;

export function login(credentials: Credentials): Promise<CurrentUser> {
  return apiPost("/api/auth/login", credentials, parseUser);
}

export async function logout(): Promise<void> {
  await apiFetch("/api/auth/logout", { method: "POST" });
}

export function register(data: Registration): Promise<CurrentUser> {
  return apiPost("/api/auth/register", data, parseUser);
}

// Must match RoleName in the backend
export const Role = {
  Organizer: "Organizer",
  Admin: "Administrator",
} as const;
export type Role = (typeof Role)[keyof typeof Role];

export const isOrganizer = (user: CurrentUser | null | undefined) =>
  hasRole(user, Role.Organizer);

export const isAdmin = (user: CurrentUser | null | undefined) =>
  hasRole(user, Role.Admin);

// --------------- Hooks --------------- \\

const meKey = ["me"] as const;
export const useMe = () => useQuery({ queryKey: meKey, queryFn: getMe });

export function useLogin() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: login,
    onSuccess: (user) => resetSession(queryClient, user),
  });
}
export function useLogout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: logout,
    onSuccess: () => resetSession(queryClient, null),
  });
}

export function useRegister() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: register,
    onSuccess: (user) => resetSession(queryClient, user),
  });
}
