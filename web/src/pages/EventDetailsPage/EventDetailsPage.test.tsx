import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router-dom";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import EventDetailsPage from "./EventDetailsPage";

const t = messages.de;
const eventId = "22222222-2222-2222-2222-222222222222";

const detail = {
  eventId,
  eventTitle: "Jazzabend",
  eventImageUrl: "/media/events/abc.webp",
  eventDescription: "Live im Keller",
  eventLocation: "Theater Kassel",
  eventStartsAt: "2030-06-15T18:00:00Z",
  doorsOpenAt: "2030-06-15T17:00:00Z",
  maxParticipants: 50,
  availableSpots: 12,
  categoryId: 1,
  categoryName: "Konzert",
  categoryDescription: null,
  organizerId: "33333333-3333-3333-3333-333333333333",
  organizerName: "Organizer",
  isCancelled: false,
};

// Nobody logged in – the booking box then only needs /me.
const anonymous = http.get("/api/auth/me", () =>
  HttpResponse.json({ status: 401 }, { status: 401 }),
);

function renderDetails() {
  renderPage(
    <Routes>
      <Route path="/events/:id" element={<EventDetailsPage />} />
    </Routes>,
    { route: `/events/${eventId}` },
  );
}

describe("EventDetailsPage", () => {
  it("loads the event of the URL and shows its details and booking box", async () => {
    let requestedId: string | undefined;
    server.use(
      anonymous,
      http.get("/api/events/:id", ({ params }) => {
        requestedId = String(params.id);
        return HttpResponse.json(detail);
      }),
    );
    renderDetails();

    expect(
      await screen.findByRole("heading", { name: "Jazzabend" }),
    ).toBeInTheDocument();
    expect(requestedId).toBe(eventId);
    expect(screen.getByText(/Theater Kassel/)).toBeInTheDocument();
    expect(screen.getByText(/Konzert/)).toBeInTheDocument();
    expect(
      screen.getByText(`${t["event.availableSpots"]}: 12`),
    ).toBeInTheDocument();
    expect(document.querySelector("img")).toHaveAttribute(
      "src",
      "/media/events/abc.webp",
    );
    expect(
      await screen.findByRole("link", { name: t["booking.loginToBook"] }),
    ).toBeInTheDocument();
  });

  it("renders no image when the event has none", async () => {
    server.use(
      anonymous,
      http.get("/api/events/:id", () =>
        HttpResponse.json({ ...detail, eventImageUrl: null }),
      ),
    );
    renderDetails();

    await screen.findByRole("heading", { name: "Jazzabend" });
    expect(document.querySelector("img")).toBeNull();
  });

  it("shows the translated not-found error for an unknown event", async () => {
    server.use(
      http.get("/api/events/:id", () =>
        HttpResponse.json(
          { status: 404, code: "event.not_found" },
          { status: 404 },
        ),
      ),
    );
    renderDetails();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["error.event.not_found"],
    );
  });

  it("shows the server error and recovers on retry", async () => {
    let fail = true;
    server.use(
      anonymous,
      http.get("/api/events/:id", () =>
        fail
          ? HttpResponse.json({ status: 500 }, { status: 500 })
          : HttpResponse.json(detail),
      ),
    );
    renderDetails();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["error.status.server"],
    );

    fail = false;
    await userEvent.click(
      screen.getByRole("button", { name: t["common.retry"] }),
    );

    expect(
      await screen.findByRole("heading", { name: "Jazzabend" }),
    ).toBeInTheDocument();
  });
});
