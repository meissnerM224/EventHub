import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router-dom";
import { screen } from "@testing-library/react";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import { messages } from "../../i18n/locale";
import RequireAuth from "./RequireAuth";
import { Role } from "../../api/auth";

const t = messages.de;

function renderProtected(role?: Role) {
  return renderPage(
    <Routes>
      <Route
        path="/account"
        element={
          <RequireAuth role={role}>
            <p>Geschützt</p>
          </RequireAuth>
        }
      />
      <Route path="/login" element={<p>Loginseite</p>} />
    </Routes>,
    { route: "/account" },
  );
}

function mockUser(roles: string[]) {
  server.use(
    http.get("/api/auth/me", () =>
      HttpResponse.json({
        userId: "u-1",
        displayName: "Test",
        roles,
        expiresAt: "2030-01-01T00:00:00Z",
      }),
    ),
  );
}

describe("RequireAuth", () => {
  it("shows a translated loading label while checking the session", () => {
    server.use(http.get("/api/auth/me", () => new Promise(() => {})));
    renderProtected();

    expect(screen.getByRole("status")).toHaveTextContent(t["common.loading"]);
  });

  it("redirects to login when the user is not logged in", async () => {
    server.use(
      http.get("/api/auth/me", () =>
        HttpResponse.json({ status: 401 }, { status: 401 }),
      ),
    );
    renderProtected();

    expect(await screen.findByText("Loginseite")).toBeInTheDocument();
  });

  it("shows an error instead of redirecting when the server fails", async () => {
    server.use(
      http.get("/api/auth/me", () =>
        HttpResponse.json({ status: 500 }, { status: 500 }),
      ),
    );
    renderProtected();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["error.status.server"],
    );
    expect(screen.queryByText("Loginseite")).not.toBeInTheDocument();
  });

  it("renders the page for a logged-in user", async () => {
    server.use(
      http.get("/api/auth/me", () =>
        HttpResponse.json({
          userId: "u-1",
          displayName: "Test",
          roles: ["Participant"],
          expiresAt: "2030-01-01T00:00:00Z",
        }),
      ),
    );
    renderProtected();

    expect(await screen.findByText("Geschützt")).toBeInTheDocument();
  });

  it("shows a no-access message when the user lacks the required role", async () => {
    mockUser(["Participant"]);
    renderProtected(Role.Organizer);

    expect(await screen.findByText(t["auth.forbidden"])).toBeInTheDocument();
    expect(screen.queryByText("Geschützt")).not.toBeInTheDocument();
  });

  it("renders the page when the user has the required role", async () => {
    mockUser(["Organizer"]);
    renderProtected(Role.Organizer);

    expect(await screen.findByText("Geschützt")).toBeInTheDocument();
  });
});
