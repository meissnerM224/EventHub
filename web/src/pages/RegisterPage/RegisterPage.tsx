import { useState, type FormEvent } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useIntl } from "react-intl";
import { useRegister, useMe } from "../../api/auth";
import { errorMessageId } from "../../api/errors.ts";
import styles from "./RegisterPage.module.css";
import { TextField } from "../../components/Field/Field";
import { Button } from "../../components/Button/Button";
import Banner from "../../components/Banner/Banner";

export default function RegisterPage() {
  const intl = useIntl();
  const { data: user, isPending } = useMe();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";
  const register = useRegister();
  const [passwordMismatch, setPasswordMismatch] = useState(false);

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const password = String(form.get("password"));
    const mismatch = password !== String(form.get("passwordConfirm"));
    setPasswordMismatch(mismatch);
    if (mismatch) return;

    register.mutate(
      {
        email: String(form.get("email")),
        password,
        displayName: String(form.get("displayName")),
      },
      //   useMe() make the rerouting in onSuccess obsolete
      // { onSuccess: () => navigate("/") },
    );
  }

  const errorId = passwordMismatch
    ? "auth.register.passwordMismatch"
    : register.error
      ? errorMessageId(register.error)
      : null;

  if (isPending) return null;
  if (user) return <Navigate to={from} replace={true} />;
  return (
    <div className={styles.page}>
      <form className={styles.form} onSubmit={handleSubmit}>
        <h1>{intl.formatMessage({ id: "auth.register.title" })}</h1>
        <TextField
          label={intl.formatMessage({ id: "auth.email" })}
          name="email"
          type="email"
          autoComplete="email"
          required
        />
        <TextField
          label={intl.formatMessage({ id: "auth.displayName" })}
          name="displayName"
          autoComplete="nickname"
          required
          maxLength={100}
        />
        <TextField
          label={intl.formatMessage({ id: "auth.password" })}
          hint={intl.formatMessage({ id: "auth.password.hint" })}
          name="password"
          type="password"
          autoComplete="new-password"
          required
          minLength={8}
        />
        <TextField
          label={intl.formatMessage({ id: "auth.passwordConfirm" })}
          name="passwordConfirm"
          type="password"
          autoComplete="new-password"
          required
        />
        {errorId && <Banner messageId={errorId} />}
        <Button type="submit" disabled={register.isPending}>
          {intl.formatMessage({
            id: register.isPending ? "common.loading" : "auth.register.submit",
          })}
        </Button>
      </form>
    </div>
  );
}
