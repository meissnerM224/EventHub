import type { FormEvent } from "react";
import { Link, Navigate, useLocation } from "react-router-dom";
import { useIntl } from "react-intl";
import { useLogin, useMe } from "../../api/auth";
import styles from "./LoginPage.module.css";
import { errorMessageId } from "../../api/errors.ts";
import { TextField } from "../../components/Field/Field";
import { Button } from "../../components/Button/Button";
import Banner from "../../components/Banner/Banner";

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

  if (isPending) return null;
  if (user) return <Navigate to={from} replace={true} />;
  return (
    <div className={styles.page}>
      <form className={styles.form} onSubmit={handleSubmit}>
        <h1>{intl.formatMessage({ id: "auth.login.title" })}</h1>
        <TextField
          label={intl.formatMessage({ id: "auth.email" })}
          name="email"
          type="email"
          autoComplete="email"
          required
        />
        <TextField
          label={intl.formatMessage({ id: "auth.password" })}
          name="password"
          type="password"
          autoComplete="current-password"
          required
        />
        <Link to="/forgot-password" className={styles.forgot}>
          {intl.formatMessage({ id: "auth.login.forgotPassword" })}
        </Link>
        {login.error && <Banner messageId={errorMessageId(login.error)} />}
        <Button type="submit" disabled={login.isPending}>
          {intl.formatMessage({
            id: login.isPending ? "common.loading" : "auth.login.submit",
          })}
        </Button>
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
