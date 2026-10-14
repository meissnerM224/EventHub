import { Link, NavLink } from "react-router-dom";
import styles from "./Header.module.css";

export default function Header() {
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
          Anmelden
        </Link>
      </div>
    </header>
  );
}
