import { Link, NavLink } from "react-router-dom";
import styles from "./Header.module.css";
import { useIntl } from "react-intl";

export default function Header() {
  const intl = useIntl();

  return (
    <header className={styles.header}>
      <div className={styles.inner}>
        <Link to="/" className={styles.logo}>
          EventHub
        </Link>

        <nav className={styles.nav}>
          <NavLink
            to="/"
            className={({ isActive }) =>
              isActive ? styles.active : styles.link
            }
          >
            Events
          </NavLink>
        </nav>

        <Link to="/login" className={styles.cta}>
          {intl.formatMessage({ id: "login" })}
        </Link>
      </div>
    </header>
  );
}
