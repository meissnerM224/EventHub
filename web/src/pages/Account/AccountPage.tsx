import { NavLink, Outlet } from "react-router-dom";
import { useIntl } from "react-intl";
import { useMe } from "../../api/auth";
import styles from "./Account.module.css";

export default function AccountPage() {
  const intl = useIntl();
  const { data: me } = useMe();

  const tabClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? styles.tabActive : styles.tab;

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1>{intl.formatMessage({ id: "account.title" })}</h1>
        {me && (
          <p>
            {intl.formatMessage(
              { id: "account.greeting" },
              { name: me.displayName },
            )}
          </p>
        )}
      </div>

      <nav className={styles.tabs}>
        <NavLink to="bookings" className={tabClass}>
          {intl.formatMessage({ id: "account.nav.bookings" })}
        </NavLink>
        <NavLink to="settings" className={tabClass}>
          {intl.formatMessage({ id: "account.nav.settings" })}
        </NavLink>
      </nav>

      <Outlet />
    </div>
  );
}
