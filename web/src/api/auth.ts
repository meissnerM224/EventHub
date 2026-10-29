import { ApiError, apiFetch, apiGet, apiPost } from "./client";
import { currentUserSchema, type CurrentUser } from "./types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

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

export function login(credentials: Credentials): Promise<CurrentUser> {
  return apiPost("/api/auth/login", credentials, parseUser);
}

export async function logout(): Promise<void> {
  await apiFetch("/api/auth/logout", { method: "POST" });
}

export function register(data: Registration): Promise<CurrentUser> {
  return apiPost("/api/auth/register", data, parseUser);
}

export const isOrganizer = (user: CurrentUser | null | undefined) =>
  user?.roles.includes("Organizer") ?? false;

export const isAdmin = (user: CurrentUser | null | undefined) =>
  user?.roles.includes("Administrator") ?? false;

// --------------- Hooks --------------- \\

const meKey = ["me"] as const;
export const useMe = () => useQuery({ queryKey: meKey, queryFn: getMe });

export function useLogin() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: login,
    onSuccess: (user) => queryClient.setQueryData(meKey, user),
  });
}
export function useLogout() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: logout,
    onSuccess: () => {
      queryClient.clear();
      queryClient.setQueryData(meKey, null);
    },
  });
}

export function useRegister() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: register,
    onSuccess: (user) => queryClient.setQueryData(meKey, user),
  });
}
