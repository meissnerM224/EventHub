import type { FormEvent } from "react";
import { Link, Navigate, useLocation } from "react-router-dom";
import { useIntl } from "react-intl";
import { useLogin, useMe } from "../../api/auth";
import styles from "./LoginPage.module.css";
import { errorMessageId } from "../../api/errors.ts";

export default function LoginPage() {
  const intl = useIntl();
  const { data: user, isPending } = useMe();
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
      // is obsulete, when user is set in useMe()
      // { onSuccess: () => navigate(from, { replace: true }) },
    );
  }

  const errorText = login.error
    ? intl.formatMessage({ id: errorMessageId(login.error) })
    : null;
  if (isPending) return null;
  if (user) return <Navigate to={from} replace={true} />;
  return (
    <div className={styles.page}>
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
    </div>
  );
}
