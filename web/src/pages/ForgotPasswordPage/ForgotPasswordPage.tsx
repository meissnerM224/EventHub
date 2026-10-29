import { Link } from "react-router-dom";
import { useIntl } from "react-intl";
import styles from "../LoginPage/LoginPage.module.css";

// TODO: replace with a real form once the API offers a reset endpoint
export default function ForgotPasswordPage() {
  const intl = useIntl();

  return (
    <main className={styles.page}>
      <div className={styles.form}>
        <h1>{intl.formatMessage({ id: "auth.forgot.title" })}</h1>
        <p>{intl.formatMessage({ id: "auth.forgot.unavailable" })}</p>
        <Link to="/login">
          {intl.formatMessage({ id: "auth.forgot.back" })}
        </Link>
      </div>
    </main>
  );
}
