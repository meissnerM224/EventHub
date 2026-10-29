import { Route, Routes } from "react-router-dom";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderPage } from "../../test/render";
import LoginPage from "./LoginPage";

function renderLogin() {
  return renderPage(
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<p>Registrierungsseite</p>} />
      <Route path="/forgot-password" element={<p>Passwort-Seite</p>} />
    </Routes>,
    { route: "/login" },
  );
}

describe("LoginPage", () => {
  it("navigates to the register page", async () => {
    renderLogin();

    await userEvent.click(screen.getByRole("link", { name: /registrieren/i }));

    expect(screen.getByText("Registrierungsseite")).toBeInTheDocument();
  });

  it("navigates to the forgot-password page", async () => {
    renderLogin();

    await userEvent.click(
      screen.getByRole("link", { name: /passwort vergessen/i }),
    );

    expect(screen.getByText("Passwort-Seite")).toBeInTheDocument();
  });
});
