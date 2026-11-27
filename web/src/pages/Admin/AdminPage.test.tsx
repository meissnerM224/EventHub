import { http, HttpResponse } from "msw";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import AdminPage from "./AdminPage";

const t = messages.de;

const anna = {
  userId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Anna",
  email: "anna@test.de",
  roles: ["Participant"],
};

describe("AdminPage", () => {
  it("grants the organizer role and shows the new state", async () => {
    let users = [anna];
    let grantedFor: string | undefined;
    server.use(
      http.get("/api/admin/users", () => HttpResponse.json(users)),
      http.get("/api/categories", () => HttpResponse.json([])),
      http.put("/api/admin/users/:userId/roles/organizer", ({ params }) => {
        grantedFor = String(params.userId);
        users = [{ ...anna, roles: ["Participant", "Organizer"] }];
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage(<AdminPage />);

    const row = (await screen.findByRole("cell", { name: "Anna" })).closest(
      "tr",
    )!;
    await userEvent.click(
      within(row).getByRole("button", {
        name: t["admin.users.grantOrganizer"],
      }),
    );

    expect(
      await within(row).findByRole("button", {
        name: t["admin.users.revokeOrganizer"],
      }),
    ).toBeInTheDocument();
    expect(grantedFor).toBe(anna.userId);
  });

  it("creates a category and shows the duplicate error code", async () => {
    let categories = [{ id: 1, name: "Konzert", description: "" }];
    server.use(
      http.get("/api/admin/users", () => HttpResponse.json([])),
      http.get("/api/categories", () => HttpResponse.json(categories)),
      http.post("/api/categories", async ({ request }) => {
        const body = (await request.json()) as { name: string };
        if (categories.some((c) => c.name === body.name)) {
          return HttpResponse.json(
            { status: 409, code: "category.already_exists" },
            { status: 409 },
          );
        }
        const created = { id: 2, name: body.name, description: "" };
        categories = [...categories, created];
        return HttpResponse.json(created, { status: 201 });
      }),
    );
    const user = userEvent.setup();
    renderPage(<AdminPage />);
    const name = await screen.findByLabelText(t["admin.categories.name"]);
    const submit = screen.getByRole("button", {
      name: t["admin.categories.submit"],
    });

    await user.type(name, "Lesung");
    await user.click(submit);
    expect(await screen.findByText("Lesung")).toBeInTheDocument();
    // reset() runs in onSuccess – wait for it, or it clears the next input
    await waitFor(() => expect(name).toHaveValue(""));

    await user.clear(name);
    await user.type(name, "Konzert");
    await user.click(submit);
    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["error.category.already_exists"],
    );
  });
});
