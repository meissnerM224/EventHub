import { http, HttpResponse } from "msw";
import { screen } from "@testing-library/react";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import CreateNewEvent from "./CreateNewEvent";
import { Route, Routes } from "react-router-dom";
import userEvent from "@testing-library/user-event";

const t = messages.de;

describe("CreateNewEvent", () => {
  it("shows the categories from the API in the dropdown", async () => {
    server.use(
      http.get("/api/categories", () =>
        HttpResponse.json([
          { id: 1, name: "Konzert", description: "" },
          { id: 2, name: "Workshop", description: "" },
        ]),
      ),
    );
    renderPage(<CreateNewEvent />);

    expect(
      await screen.findByRole("option", { name: "Konzert" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "Workshop" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("combobox", { name: t["createEvent.category"] }),
    ).toBeEnabled();
  });

  it("disables the dropdown when categories cannot be loaded", async () => {
    server.use(
      http.get("/api/categories", () =>
        HttpResponse.json({ status: 500 }, { status: 500 }),
      ),
    );
    renderPage(<CreateNewEvent />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["createEvent.category.loadError"],
    );
    expect(
      screen.getByRole("combobox", { name: t["createEvent.category"] }),
    ).toBeDisabled();
  });
  describe("submit", () => {
    const categoriesHandler = http.get("/api/categories", () =>
      HttpResponse.json([{ id: 1, name: "Konzert", description: "" }]),
    );

    beforeEach(() => {
      // jsdom has no object URLs
      URL.createObjectURL = vi.fn(() => "blob:preview");
      URL.revokeObjectURL = vi.fn();
    });

    function renderForm() {
      renderPage(
        <Routes>
          <Route path="/events/new" element={<CreateNewEvent />} />
          <Route path="/events/:id" element={<p>Detailseite</p>} />
        </Routes>,
        { route: "/events/new" },
      );
    }

    async function fillForm() {
      const user = userEvent.setup();
      await screen.findByRole("option", { name: "Konzert" });
      await user.type(
        screen.getByLabelText(t["createEvent.eventTitle"]),
        "Jazzabend",
      );
      await user.type(
        screen.getByLabelText(t["createEvent.description"]),
        "Live im Keller",
      );
      await user.type(screen.getByLabelText(t["createEvent.location"]), "Kiel");
      await user.selectOptions(
        screen.getByLabelText(t["createEvent.category"]),
        "1",
      );
      await user.type(
        screen.getByLabelText(t["createEvent.startsAt"]),
        "2030-06-15T20:00",
      );
      await user.type(
        screen.getByLabelText(t["createEvent.doorsOpenAt"]),
        "2030-06-15T19:00",
      );
      await user.type(
        screen.getByLabelText(t["createEvent.maxParticipants"]),
        "50",
      );
      return user;
    }

    it("uploads the image, creates the event and opens its page", async () => {
      let createdBody: Record<string, unknown> | undefined;
      server.use(
        categoriesHandler,
        http.post("/api/uploads/images", () =>
          HttpResponse.json({ url: "/media/events/abc.webp" }),
        ),
        http.post("/api/events", async ({ request }) => {
          createdBody = (await request.json()) as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: "44444444-4444-4444-4444-444444444444",
              title: "Jazzabend",
              location: "Kiel",
              categoryName: "Konzert",
              startsAt: "2030-06-15T18:00:00Z",
              availableSpots: 50,
              imageUrl: "/media/events/abc.webp",
            },
            { status: 201 },
          );
        }),
      );
      renderForm();
      const user = await fillForm();
      await user.upload(
        screen.getByLabelText(t["createEvent.image"]),
        new File(["x"], "bild.png", { type: "image/png" }),
      );

      expect(
        screen.getByAltText(t["createEvent.image.preview"]),
      ).toBeInTheDocument();

      await user.click(
        screen.getByRole("button", { name: t["createEvent.submit"] }),
      );

      expect(await screen.findByText("Detailseite")).toBeInTheDocument();
      expect(createdBody).toMatchObject({
        title: "Jazzabend",
        categoryId: 1,
        maxParticipants: 50,
        image: "/media/events/abc.webp",
      });
      expect(createdBody?.startAt).toMatch(/Z$/);
    });

    it("shows the translated error in a banner", async () => {
      server.use(
        categoriesHandler,
        http.post("/api/events", () =>
          HttpResponse.json(
            { status: 400, code: "event.invalid_dates" },
            { status: 400 },
          ),
        ),
      );
      renderForm();
      const user = await fillForm();

      await user.click(
        screen.getByRole("button", { name: t["createEvent.submit"] }),
      );

      expect(await screen.findByRole("alert")).toHaveTextContent(
        t["error.event.invalid_dates"],
      );
    });
  });
});
