import { Link } from "react-router-dom";
import { useIntl } from "react-intl";
import styles from "./Account.module.css";

export default function MyBookingsPage() {
  const intl = useIntl();

  return (
    <div className={styles.container}>
      <div className={styles.body}>
        <h2>{intl.formatMessage({ id: "account.nav.bookings" })}</h2>
        <p className={styles.muted}>
          {intl.formatMessage({ id: "account.bookings.empty" })}
        </p>
        <Link to="/" className={styles.cta}>
          {intl.formatMessage({ id: "account.bookings.browse" })}
        </Link>
      </div>
    </div>
  );
}
