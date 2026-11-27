import { ApiError } from "./client";
import { handleUnauthorized, queryClient, resetSession } from "./queryClient";

const anna = {
  userId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Anna",
  email: "anna@test.de",
  roles: ["Participant"],
  expiresAt: "2030-01-01T00:00:00Z",
};
const bookingsKey = ["me", "bookings"];

afterEach(() => queryClient.clear());

describe("resetSession", () => {
  it("drops the previous user's data and sets the new user", () => {
    queryClient.setQueryData(bookingsKey, [{ eventId: "e1" }]);

    resetSession(queryClient, { ...anna, displayName: "Bernd" });

    expect(queryClient.getQueryData(bookingsKey)).toBeUndefined();
    expect(queryClient.getQueryData(["me"])).toMatchObject({
      displayName: "Bernd",
    });
  });
});

describe("handleUnauthorized", () => {
  it("clears user data when the session of a logged-in user expires", () => {
    queryClient.setQueryData(["me"], anna);
    queryClient.setQueryData(bookingsKey, [{ eventId: "e1" }]);

    handleUnauthorized(new ApiError(401, "Unauthorized"));

    expect(queryClient.getQueryData(["me"])).toBeNull();
    expect(queryClient.getQueryData(bookingsKey)).toBeUndefined();
  });

  it("keeps public data on a 401 while nobody is logged in (wrong password)", () => {
    queryClient.setQueryData(["me"], null);
    queryClient.setQueryData(["events"], [{ eventId: "e1" }]);

    handleUnauthorized(new ApiError(401, "Unauthorized"));

    expect(queryClient.getQueryData(["events"])).toEqual([{ eventId: "e1" }]);
  });

  it("ignores other errors", () => {
    queryClient.setQueryData(["me"], anna);

    handleUnauthorized(new ApiError(403, "Forbidden"));

    expect(queryClient.getQueryData(["me"])).toEqual(anna);
  });
});
