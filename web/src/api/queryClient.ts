import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import { ApiError } from "./client";
import type { CurrentUser } from "./types";

const isMe = (key: readonly unknown[]) => key.length === 1 && key[0] === "me";

// Every user change (login, register, logout, expired session) starts from an empty cache.
// Otherwise user-scoped data like ["me", "bookings"] of the previous user stays visible.
// resetQueries (not clear) so ["me"] keeps its observers: no extra /me request,
// and lists that are on screen refetch for the new user right away.
export function resetSession(client: QueryClient, user: CurrentUser | null) {
  client.setQueryData(["me"], user);
  void client.resetQueries({ predicate: (query) => !isMe(query.queryKey) });
}

// One place for "session expired": any 401 means nobody is logged in anymore.
// The header then shows "Anmelden" instead of a stale name.
// A 401 from the login form (wrong password) lands here too – nobody is logged in
// then, so the cache is left alone instead of refetching every public list.
export function handleUnauthorized(error: unknown) {
  if (!(error instanceof ApiError) || error.status !== 401) return;
  if (queryClient.getQueryData(["me"])) resetSession(queryClient, null);
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({ onError: handleUnauthorized }),
  mutationCache: new MutationCache({ onError: handleUnauthorized }),
  defaultOptions: {
    queries: { staleTime: 30_000 },
  },
});
