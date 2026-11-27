import { http, HttpResponse } from "msw";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import EventPage from "./EventPage";

const t = messages.de;

const event = (id: string, title: string) => ({
  id,
  title,
  location: "Kassel",
  categoryName: "Konzert",
  startsAt: "2030-06-15T18:00:00Z",
  availableSpots: 10,
  imageUrl: null,
});

const serverError = () => HttpResponse.json({ status: 500 }, { status: 500 });

describe("EventPage", () => {
  it("shows a card per event, linked to its detail page", async () => {
    server.use(
      http.get("/api/events", () =>
        HttpResponse.json([event("e1", "Jazzabend"), event("e2", "Workshop")]),
      ),
    );
    renderPage(<EventPage />);

    expect(
      await screen.findByRole("heading", { name: "Jazzabend" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Workshop" }),
    ).toBeInTheDocument();
    expect(
      screen.getAllByRole("link").map((a) => a.getAttribute("href")),
    ).toEqual(["/events/e1", "/events/e2"]);
  });

  it("shows the empty state and loads again on refresh", async () => {
    let events: unknown[] = [];
    server.use(http.get("/api/events", () => HttpResponse.json(events)));
    renderPage(<EventPage />);

    expect(
      await screen.findByText(t["events.empty.title"]),
    ).toBeInTheDocument();

    events = [event("e1", "Jazzabend")];
    await userEvent.click(
      screen.getByRole("button", { name: t["common.refresh"] }),
    );

    expect(
      await screen.findByRole("heading", { name: "Jazzabend" }),
    ).toBeInTheDocument();
  });

  it("shows the server error and recovers on retry", async () => {
    let fail = true;
    server.use(
      http.get("/api/events", () =>
        fail ? serverError() : HttpResponse.json([event("e1", "Jazzabend")]),
      ),
    );
    renderPage(<EventPage />);

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

  it("shows an error instead of broken cards when the response has the wrong shape", async () => {
    // e.g. after a backend rename – zod rejects it instead of rendering "undefined".
    server.use(
      http.get("/api/events", () => HttpResponse.json([{ name: "Jazzabend" }])),
    );
    vi.spyOn(console, "error").mockImplementation(() => {});
    renderPage(<EventPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["common.error"],
    );
    expect(screen.queryByRole("heading")).not.toBeInTheDocument();
  });
});
