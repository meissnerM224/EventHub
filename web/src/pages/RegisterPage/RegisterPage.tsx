import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useIntl } from "react-intl";
import { useRegister } from "../../api/auth";
import { ApiError } from "../../api/client";
import styles from "./RegisterPage.module.css";

export default function RegisterPage() {
  const intl = useIntl();
  const navigate = useNavigate();
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
      { onSuccess: () => navigate("/") },
    );
  }

  const errorText = passwordMismatch
    ? intl.formatMessage({ id: "auth.register.passwordMismatch" })
    : register.error instanceof ApiError && register.error.status === 409
      ? intl.formatMessage({ id: "auth.register.emailTaken" })
      : register.error
        ? intl.formatMessage({ id: "common.error" })
        : null;

  return (
    <div className={styles.page}>
      <form className={styles.form} onSubmit={handleSubmit}>
        <h1>{intl.formatMessage({ id: "auth.register.title" })}</h1>

        <label>
          {intl.formatMessage({ id: "auth.email" })}
          <input name="email" type="email" autoComplete="email" required />
        </label>

        <label>
          {intl.formatMessage({ id: "auth.displayName" })}
          <input
            name="displayName"
            autoComplete="nickname"
            required
            maxLength={100}
          />
        </label>

        <label>
          {intl.formatMessage({ id: "auth.password" })}
          <input
            name="password"
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            aria-describedby="password-hint"
          />
        </label>
        <small id="password-hint" className={styles.hint}>
          {intl.formatMessage({ id: "auth.password.hint" })}
        </small>

        <label>
          {intl.formatMessage({ id: "auth.passwordConfirm" })}
          <input
            name="passwordConfirm"
            type="password"
            autoComplete="new-password"
            required
          />
        </label>

        {errorText && (
          <p role="alert" className={styles.error}>
            {errorText}
          </p>
        )}

        <button
          type="submit"
          className={styles.submit}
          disabled={register.isPending}
        >
          {intl.formatMessage({
            id: register.isPending ? "common.loading" : "auth.register.submit",
          })}
        </button>
      </form>
    </div>
  );
}
