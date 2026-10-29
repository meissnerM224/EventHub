import type { FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useIntl } from "react-intl";
import { useLogin } from "../../api/auth";
import { ApiError } from "../../api/client";
import styles from "./LoginPage.module.css";

// EMAIL= admin@admin.de
// PASSWORD=  Test1234!

export default function LoginPage() {
  const intl = useIntl();
  const navigate = useNavigate();
  const login = useLogin();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    login.mutate(
      {
        email: String(form.get("email")),
        password: String(form.get("password")),
      },
      { onSuccess: () => navigate(from, { replace: true }) },
    );
  }

  const errorText =
    login.error instanceof ApiError && login.error.status === 401
      ? intl.formatMessage({ id: "auth.login.invalid" })
      : login.error
        ? intl.formatMessage({ id: "common.error" })
        : null;
  return (
    <main className={styles.page}>
      <form className={styles.form} onSubmit={handleSubmit}>
        <h1>{intl.formatMessage({ id: "auth.login.title" })}</h1>

        <label>
          {intl.formatMessage({ id: "auth.email" })}
          <input name="email" type="email" autoComplete="email" required />
        </label>

        <label>
          {intl.formatMessage({ id: "auth.password" })}
          <input
            name="password"
            type="password"
            autoComplete="current-password"
            required
          />
        </label>

        <Link to="/forgot-password" className={styles.forgot}>
          {intl.formatMessage({ id: "auth.login.forgotPassword" })}
        </Link>

        {errorText && (
          <p role="alert" className={styles.error}>
            {errorText}
          </p>
        )}

        <button
          type="submit"
          className={styles.submit}
          disabled={login.isPending}
        >
          {intl.formatMessage({
            id: login.isPending ? "common.loading" : "auth.login.submit",
          })}
        </button>

        <p className={styles.switch}>
          {intl.formatMessage({ id: "auth.login.noAccount" })}{" "}
          <Link to="/register">
            {intl.formatMessage({ id: "auth.register.submit" })}
          </Link>
        </p>
      </form>
    </main>
  );
}
