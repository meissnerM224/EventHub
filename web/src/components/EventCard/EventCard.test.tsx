import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import EventCard from "./EventCard";

const defaults = {
  id: "c7432f4a-5a21-4f64-ad51-eac9860e11e9",
  title: "Sommerfest im Park",
  location: "Kassel",
  categoryName: "Konzert",
  startsAt: "2026-07-18T17:00:00Z",
  availableSpots: 12,
  imageUrl: null,
};

function renderCard(overrides: Partial<typeof defaults> = {}) {
  return render(
    <MemoryRouter>
      <EventCard {...defaults} {...overrides} />
    </MemoryRouter>,
  );
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

  it("deaktiviert den Button, wenn keine Plätze frei sind", () => {
    renderCard({ availableSpots: 0 });

    expect(screen.getByRole("button", { name: "Sold out" })).toBeDisabled();
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
