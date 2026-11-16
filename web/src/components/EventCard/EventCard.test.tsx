import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import EventCard, { type EventCardProps } from "./EventCard";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";

const t = messages.de;

const defaults: EventCardProps = {
  id: "c7432f4a-5a21-4f64-ad51-eac9860e11e9",
  title: "Sommerfest im Park",
  location: "Kassel",
  categoryName: "Konzert",
  startsAt: "2026-07-18T17:00:00Z",
  availableSpots: 12,
  imageUrl: null,
};

function renderCard(overrides: Partial<EventCardProps> = {}) {
  return renderPage(<EventCard {...defaults} {...overrides} />);
}

describe("EventCard", () => {
  it("zeigt Titel, Ort und Kategorie", () => {
    renderCard();

    expect(
      screen.getByRole("heading", { name: "Sommerfest im Park" }),
    ).toBeInTheDocument();
    expect(screen.getByText(/Kassel/)).toBeInTheDocument();
    expect(screen.getByText(/Konzert/)).toBeInTheDocument();
  });

  it("verlinkt auf die Detailseite", () => {
    renderCard();

    expect(screen.getByRole("link")).toHaveAttribute(
      "href",
      `/events/${defaults.id}`,
    );
  });

  it("zeigt 'Ausverkauft', wenn keine Plätze frei sind", () => {
    renderCard({ availableSpots: 0 });

    expect(screen.getByText(t["event.soldOut"])).toBeInTheDocument();
    expect(screen.queryByText(t["event.booking"])).not.toBeInTheDocument();
  });

  it("enthält keinen Button innerhalb des Links", () => {
    renderCard();

    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("zeigt kein Bild, wenn imageUrl fehlt", () => {
    const { container } = renderCard({ imageUrl: null });
    // expect(screen.getByRole('presentation')).toHaveAttribute('src', '…')
    expect(container.querySelector("img")).not.toBeInTheDocument();
  });

  it("zeigt ein Bild, wenn imageUrl gesetzt ist", () => {
    const { container } = renderCard({ imageUrl: "/media/events/abc.webp" });
    // expect(screen.getByRole('presentation')).toHaveAttribute('src', '…')
    expect(container.querySelector("img")).toHaveAttribute(
      "src",
      "/media/events/abc.webp",
    );
  });
});
