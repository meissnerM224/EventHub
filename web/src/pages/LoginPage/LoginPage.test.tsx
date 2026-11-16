import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router-dom";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import LoginPage from "./LoginPage";

// The page renders nothing until useMe() resolves, so wait for the form.
async function renderLogin() {
  const result = renderPage(
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<p>Registrierungsseite</p>} />
      <Route path="/forgot-password" element={<p>Passwort-Seite</p>} />
    </Routes>,
    { route: "/login" },
  );
  await screen.findByRole("heading", { name: /login/i });
  return result;
}

describe("LoginPage", () => {
  beforeEach(() => {
    server.use(
      http.get("/api/auth/me", () =>
        HttpResponse.json({ status: 401 }, { status: 401 }),
      ),
    );
  });

  it("navigates to the register page", async () => {
    await renderLogin();

    await userEvent.click(screen.getByRole("link", { name: /registrieren/i }));

    expect(screen.getByText("Registrierungsseite")).toBeInTheDocument();
  });

  it("navigates to the forgot-password page", async () => {
    await renderLogin();

    await userEvent.click(
      screen.getByRole("link", { name: /passwort vergessen/i }),
    );

    expect(screen.getByText("Passwort-Seite")).toBeInTheDocument();
  });
});

it("leitet eingeloggte Nutzer zur Startseite weiter", async () => {
  server.use(
    http.get("/api/auth/me", () =>
      HttpResponse.json({
        userId: "11111111-1111-1111-1111-111111111111",
        displayName: "Test User",
        roles: ["Participant"],
        expiresAt: "2030-01-01T00:00:00Z",
      }),
    ),
  );

  renderPage(
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<p>Startseite</p>} />
    </Routes>,
    { route: "/login" },
  );

  expect(await screen.findByText("Startseite")).toBeInTheDocument();
  expect(
    screen.queryByRole("heading", { name: /login/i }),
  ).not.toBeInTheDocument();
});
