import { http, HttpResponse, delay } from "msw";
import { Route, Routes } from "react-router-dom";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { server } from "../../test/server";
import { renderPage } from "../../test/render";
import RegisterPage from "./RegisterPage";
import LoginPage from "../LoginPage/LoginPage.tsx";
import { messages } from "../../i18n/locale";

const t = messages.de;

const createdUser = {
  userId: "u-1",
  displayName: "Test",
  roles: ["Participant"],
  expiresAt: "2030-01-01T00:00:00Z",
};

// The page renders nothing until useMe() resolves, so wait for the form.
async function renderRegister() {
  const result = renderPage(
    <Routes>
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/" element={<p>Startseite</p>} />
    </Routes>,
    { route: "/register" },
  );
  await screen.findByRole("heading", { name: /registrieren/i });
  return result;
}

async function fillForm({
  email = "new@test.de",
  displayName = "Test",
  password = "Test1234!",
  passwordConfirm = password,
}: {
  email?: string;
  displayName?: string;
  password?: string;
  passwordConfirm?: string;
} = {}) {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText(/email/i), email);
  await user.type(screen.getByLabelText(/anzeigename/i), displayName);
  await user.type(screen.getByLabelText("Passwort"), password);
  await user.type(
    screen.getByLabelText(/passwort bestätigen/i),
    passwordConfirm,
  );
  await user.click(screen.getByRole("button", { name: /registrieren/i }));
}

describe("RegisterPage", () => {
  beforeEach(() => {
    server.use(
      http.get("/api/auth/me", () =>
        HttpResponse.json({ status: 401 }, { status: 401 }),
      ),
    );
  });

  it("renders all form fields", async () => {
    await renderRegister();

    expect(
      screen.getByRole("heading", { name: /registrieren/i }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeRequired();
    expect(screen.getByLabelText(/anzeigename/i)).toHaveAttribute(
      "maxLength",
      "100",
    );
    expect(screen.getByLabelText("Passwort")).toHaveAttribute("minLength", "8");
    expect(screen.getByLabelText("Passwort")).toHaveAccessibleDescription(
      /mindestens 8 zeichen/i,
    );
    expect(screen.getByLabelText(/passwort bestätigen/i)).toBeRequired();
    expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("registers without sending a role and navigates home", async () => {
    let body: unknown;
    server.use(
      http.post("/api/auth/register", async ({ request }) => {
        body = await request.json();
        return HttpResponse.json(createdUser, { status: 201 });
      }),
    );
    const { queryClient } = await renderRegister();

    await fillForm();

    expect(await screen.findByText("Startseite")).toBeInTheDocument();
    expect(body).toEqual({
      email: "new@test.de",
      password: "Test1234!",
      displayName: "Test",
    });
    expect(queryClient.getQueryData(["me"])).toEqual(createdUser);
  });

  it("shows an error and does not submit when passwords differ", async () => {
    const onRequest = vi.fn();
    server.use(
      http.post("/api/auth/register", () => {
        onRequest();
        return HttpResponse.json(createdUser, { status: 201 });
      }),
    );
    await renderRegister();

    await fillForm({ passwordConfirm: "Other1234!" });

    expect(screen.getByRole("alert")).toHaveTextContent(
      /stimmen nicht überein/i,
    );
    expect(onRequest).not.toHaveBeenCalled();
  });

  it("submits after correcting a password mismatch", async () => {
    server.use(
      http.post("/api/auth/register", () =>
        HttpResponse.json(createdUser, { status: 201 }),
      ),
    );
    await renderRegister();
    const user = userEvent.setup();

    await fillForm({ passwordConfirm: "Other1234!" });
    const confirm = screen.getByLabelText(/passwort bestätigen/i);
    await user.clear(confirm);
    await user.type(confirm, "Test1234!");
    await user.click(screen.getByRole("button", { name: /registrieren/i }));

    expect(await screen.findByText("Startseite")).toBeInTheDocument();
  });

  it("shows email-taken message on 409", async () => {
    server.use(
      http.post("/api/auth/register", () =>
        HttpResponse.json(
          { status: 409, detail: "…", code: "user.email_taken" },
          { status: 409 },
        ),
      ),
    );
    await renderRegister();

    await fillForm({ email: "taken@test.de" });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["error.user.email_taken"],
    );
    expect(screen.queryByText("Startseite")).not.toBeInTheDocument();
  });

  it("shows a generic error on server failure", async () => {
    server.use(
      http.post("/api/auth/register", () =>
        HttpResponse.json({ status: 500 }, { status: 500 }),
      ),
    );
    await renderRegister();

    await fillForm();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      t["common.error"],
    );
  });

  it("disables the submit button while the request is pending", async () => {
    server.use(
      http.post("/api/auth/register", async () => {
        await delay("infinite");
        return HttpResponse.json(createdUser, { status: 201 });
      }),
    );
    await renderRegister();

    await fillForm();

    expect(await screen.findByRole("button", { name: /lädt/i })).toBeDisabled();
  });

  it("does not submit when required fields are empty", async () => {
    const onRequest = vi.fn();
    server.use(
      http.post("/api/auth/register", () => {
        onRequest();
        return HttpResponse.json(createdUser, { status: 201 });
      }),
    );
    await renderRegister();

    await userEvent.click(
      screen.getByRole("button", { name: /registrieren/i }),
    );

    expect(onRequest).not.toHaveBeenCalled();
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
    screen.queryByRole("heading", { name: /anmelden/i }),
  ).not.toBeInTheDocument();
});
