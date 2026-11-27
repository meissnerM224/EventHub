import { http, HttpResponse } from "msw";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import MyBookingsPage from "./MyBookingsPage";

const t = messages.de;

const user = {
  userId: "11111111-1111-1111-1111-111111111111",
  displayName: "Test User",
  roles: ["Participant"],
  expiresAt: "2030-01-01T00:00:00Z",
};

const booking = (eventId: string, eventTitle: string, isCancelled = false) => ({
  bookingId: `b-${eventId}`,
  eventId,
  eventTitle,
  eventStartsAt: "2030-06-15T18:00:00Z",
  isCancelled,
  bookedAt: "2030-01-01T10:00:00Z",
});

describe("MyBookingsPage", () => {
  it("shows the empty state without bookings", async () => {
    server.use(
      http.get("/api/auth/me", () => HttpResponse.json(user)),
      http.get("/api/me/bookings", () => HttpResponse.json([])),
    );
    renderPage(<MyBookingsPage />);

    expect(
      await screen.findByText(t["account.bookings.empty"]),
    ).toBeInTheDocument();
  });

  it("lists bookings and removes one after cancelling", async () => {
    let bookings = [
      booking("e1", "Jazzabend"),
      booking("e2", "Workshop", true),
    ];
    server.use(
      http.get("/api/auth/me", () => HttpResponse.json(user)),
      http.get("/api/me/bookings", () => HttpResponse.json(bookings)),
      http.delete("/api/events/e1/bookings/me", () => {
        bookings = bookings.filter((b) => b.eventId !== "e1");
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage(<MyBookingsPage />);

    const row = (
      await screen.findByRole("link", { name: "Jazzabend" })
    ).closest("li")!;
    expect(screen.getByText(t["error.event.cancelled"])).toBeInTheDocument();
    // A cancelled event cannot be cancelled again – the backend would answer event.cancelled.
    const cancelledRow = screen
      .getByRole("link", { name: "Workshop" })
      .closest("li")!;
    expect(
      within(cancelledRow).queryByRole("button", { name: t["booking.cancel"] }),
    ).not.toBeInTheDocument();

    await userEvent.click(
      within(row).getByRole("button", { name: t["booking.cancel"] }),
    );

    // The list is loaded again after cancelling – wait for the new data.
    await waitFor(() =>
      expect(
        screen.queryByRole("link", { name: "Jazzabend" }),
      ).not.toBeInTheDocument(),
    );
    expect(screen.getByRole("link", { name: "Workshop" })).toBeInTheDocument();
  });
  it("moves past events to their own tab, without cancel button", async () => {
    const old = {
      ...booking("e3", "Altes Konzert"),
      eventStartsAt: "2020-01-01T18:00:00Z",
    };
    server.use(
      http.get("/api/auth/me", () => HttpResponse.json(user)),
      http.get("/api/me/bookings", () =>
        HttpResponse.json([old, booking("e1", "Jazzabend")]),
      ),
    );
    renderPage(<MyBookingsPage />);

    await screen.findByRole("link", { name: "Jazzabend" });
    expect(
      screen.queryByRole("link", { name: "Altes Konzert" }),
    ).not.toBeInTheDocument();

    await userEvent.click(
      screen.getByRole("tab", { name: t["account.bookings.past"] }),
    );

    expect(
      screen.getByRole("link", { name: "Altes Konzert" }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: t["booking.cancel"] }),
    ).not.toBeInTheDocument();
  });
});
