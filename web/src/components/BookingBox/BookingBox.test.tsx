import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router-dom";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import BookingBox from "./BookingBox";

const t = messages.de;
const eventId = "22222222-2222-2222-2222-222222222222";

const user = {
  userId: "11111111-1111-1111-1111-111111111111",
  displayName: "Test User",
  roles: ["Participant"],
  expiresAt: "2030-01-01T00:00:00Z",
};

const myBooking = {
  bookingId: "33333333-3333-3333-3333-333333333333",
  eventId,
  eventTitle: "Konzert",
  eventStartsAt: "2030-01-01T20:00:00Z",
  isCancelled: false,
  bookedAt: "2029-12-01T10:00:00Z",
};

const loggedIn = (bookings: unknown[] = []) =>
  server.use(
    http.get("/api/auth/me", () => HttpResponse.json(user)),
    http.get("/api/me/bookings", () => HttpResponse.json(bookings)),
  );

const future = "2030-06-15T18:00:00Z";

function renderBox(
  availableSpots = 5,
  isCancelled = false,
  eventStartsAt = future,
) {
  return renderPage(
    <Routes>
      <Route
        path="/events/:id"
        element={
          <BookingBox
            event={{ eventId, availableSpots, isCancelled, eventStartsAt }}
          />
        }
      />
      <Route path="/login" element={<p>Loginseite</p>} />
    </Routes>,
    { route: `/events/${eventId}` },
  );
}

describe("BookingBox", () => {
  it("links to the login when nobody is logged in", async () => {
    server.use(
      http.get("/api/auth/me", () =>
        HttpResponse.json({ status: 401 }, { status: 401 }),
      ),
    );
    renderBox();

    await userEvent.click(
      await screen.findByRole("link", { name: t["booking.loginToBook"] }),
    );

    expect(screen.getByText("Loginseite")).toBeInTheDocument();
  });

  it("books the event and then shows it as booked", async () => {
    let booked = false;
    server.use(
      http.get("/api/auth/me", () => HttpResponse.json(user)),
      http.get("/api/me/bookings", () =>
        HttpResponse.json(booked ? [myBooking] : []),
      ),
      http.post(`/api/events/${eventId}/bookings`, () => {
        booked = true;
        return HttpResponse.json({}, { status: 201 });
      }),
    );
    renderBox();

    await userEvent.click(
      await screen.findByRole("button", { name: t["event.booking"] }),
    );

    expect(await screen.findByText(t["booking.booked"])).toBeInTheDocument();
  });

  it("shows the translated error code when booking fails", async () => {
    loggedIn();
    server.use(
      http.post(`/api/events/${eventId}/bookings`, () =>
        HttpResponse.json(
          { status: 409, code: "event.fully_booked" },
          { status: 409 },
        ),
      ),
    );
    renderBox();

    await userEvent.click(
      await screen.findByRole("button", { name: t["event.booking"] }),
    );

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["error.event.fully_booked"],
    );
  });

  it("offers cancelling when already booked", async () => {
    loggedIn([myBooking]);
    renderBox();

    expect(await screen.findByText(t["booking.booked"])).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: t["booking.cancel"] }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: t["event.booking"] }),
    ).not.toBeInTheDocument();
  });

  it("shows sold out instead of a button", async () => {
    loggedIn();
    renderBox(0);

    expect(await screen.findByText(t["event.soldOut"])).toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows the cancellation instead of booking options", async () => {
    renderBox(5, true);

    expect(screen.getByText(t["error.event.cancelled"])).toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows that the event has started instead of book or cancel", async () => {
    // Booked user on a running event – the backend would reject both actions.
    loggedIn([myBooking]);
    renderBox(5, false, "2020-01-01T18:00:00Z");

    expect(
      screen.getByText(t["error.event.already_started"]),
    ).toBeInTheDocument();
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });
});
