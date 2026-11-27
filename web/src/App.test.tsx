import { http, HttpResponse } from "msw";
import { screen, within } from "@testing-library/react";
import { server } from "./test/server";
import { renderPage } from "./test/render";
import { messages } from "./i18n/locale";
import { AppRoutes } from "./App";

const t = messages.de;

const userWith = (...roles: string[]) => ({
  userId: "11111111-1111-1111-1111-111111111111",
  displayName: "Anna",
  roles: ["Participant", ...roles],
  expiresAt: "2030-01-01T00:00:00Z",
});

// Every page below the layout may load data – answer all of it with empty lists.
function api(me: ReturnType<typeof userWith> | null) {
  server.use(
    http.get("/api/auth/me", () =>
      me
        ? HttpResponse.json(me)
        : HttpResponse.json({ status: 401 }, { status: 401 }),
    ),
    http.get("/api/events", () => HttpResponse.json([])),
    http.get("/api/categories", () => HttpResponse.json([])),
    http.get("/api/me/bookings", () => HttpResponse.json([])),
    http.get("/api/admin/users", () => HttpResponse.json([])),
  );
}

const renderAt = (route: string) => renderPage(<AppRoutes />, { route });
const nav = () => screen.getByRole("navigation");

describe("routing", () => {
  // The route tree itself must be valid: an invalid nesting (e.g. an absolute
  // child path) throws while rendering and breaks every page, not only one.
  it.each([
    ["/", "link", t["nav.events"]],
    ["/login", "heading", t["auth.login.title"]],
    ["/does-not-exist", "heading", t["notFound.title"]],
  ])("renders %s", async (route, role, name) => {
    api(null);
    renderAt(route);

    expect(await screen.findByRole(role, { name })).toBeInTheDocument();
  });

  it("sends anonymous users from a protected page to the login", async () => {
    api(null);
    renderAt("/admin");

    expect(
      await screen.findByRole("heading", { name: t["auth.login.title"] }),
    ).toBeInTheDocument();
  });

  it("opens the bookings tab for /account", async () => {
    api(userWith());
    renderAt("/account");

    expect(
      await screen.findByRole("heading", { name: t["account.title"] }),
    ).toBeInTheDocument();
    expect(
      await screen.findByText(t["account.bookings.empty"]),
    ).toBeInTheDocument();
  });

  it("shows the admin page at /admin for an administrator", async () => {
    api(userWith("Administrator"));
    renderAt("/admin");

    expect(
      await screen.findByRole("heading", { name: t["admin.title"] }),
    ).toBeInTheDocument();
  });

  it.each([
    ["/admin", "Organizer"],
    ["/events/new", "Administrator"],
  ])("refuses %s without the required role", async (route, otherRole) => {
    api(userWith(otherRole));
    renderAt(route);

    expect(await screen.findByText(t["auth.forbidden"])).toBeInTheDocument();
  });
});

describe("header", () => {
  it("shows only events and login when nobody is logged in", async () => {
    api(null);
    renderAt("/");

    expect(
      await screen.findByRole("link", { name: t["common.login"] }),
    ).toBeInTheDocument();
    expect(within(nav()).getAllByRole("link")).toHaveLength(1);
  });

  it("shows name and logout but no role links for a participant", async () => {
    api(userWith());
    renderAt("/");

    expect(
      await screen.findByRole("link", { name: "Anna" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: t["common.logout"] }),
    ).toBeInTheDocument();
    expect(
      within(nav()).queryByRole("link", { name: t["nav.createEvent"] }),
    ).not.toBeInTheDocument();
    expect(
      within(nav()).queryByRole("link", { name: t["nav.admin"] }),
    ).not.toBeInTheDocument();
  });

  it("shows create event, but not admin, for an organizer", async () => {
    api(userWith("Organizer"));
    renderAt("/");

    expect(
      await within(nav()).findByRole("link", { name: t["nav.createEvent"] }),
    ).toHaveAttribute("href", "/events/new");
    expect(
      within(nav()).queryByRole("link", { name: t["nav.admin"] }),
    ).not.toBeInTheDocument();
  });

  it("shows admin, but not create event, for an administrator", async () => {
    api(userWith("Administrator"));
    renderAt("/");

    expect(
      await within(nav()).findByRole("link", { name: t["nav.admin"] }),
    ).toHaveAttribute("href", "/admin");
    expect(
      within(nav()).queryByRole("link", { name: t["nav.createEvent"] }),
    ).not.toBeInTheDocument();
  });
});
