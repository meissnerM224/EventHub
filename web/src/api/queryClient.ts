import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import { ApiError } from "./client";

// One place for "session expired": any 401 means nobody is logged in anymore.
// The header then shows "Anmelden" instead of a stale name.
// A 401 from the login form (wrong password) lands here too – harmless,
// "me" is already null at that point.
function handleUnauthorized(error: unknown) {
  if (error instanceof ApiError && error.status === 401) {
    queryClient.setQueryData(["me"], null);
  }
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({ onError: handleUnauthorized }),
  mutationCache: new MutationCache({ onError: handleUnauthorized }),
  defaultOptions: {
    queries: { staleTime: 30_000 },
  },
});
