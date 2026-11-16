import { Link, NavLink, useNavigate } from "react-router-dom";
import { useIntl } from "react-intl";
import { useLogout, useMe } from "../../api/auth";
import { isOrganizer } from "../../api/auth";
import styles from "./Header.module.css";

export default function Header() {
  const intl = useIntl();
  const navigate = useNavigate();
  const { data: me, isPending } = useMe();
  const logout = useLogout();
  return (
    <header className={styles.header}>
      <div className={styles.inner}>
        <Link to="/" className={styles.logo} translate="no">
          EventHub
        </Link>

        <nav className={styles.nav}>
          <NavLink
            to="/"
            end
            className={({ isActive }) =>
              isActive ? styles.active : styles.link
            }
          >
            {intl.formatMessage({ id: "nav.events" })}
          </NavLink>
          {isOrganizer(me) && (
            <NavLink
              to="/events/new"
              className={({ isActive }) =>
                isActive ? styles.active : styles.link
              }
            >
              {intl.formatMessage({ id: "nav.createEvent" })}
            </NavLink>
          )}
        </nav>
        {/*<></>*/}
        <div className={styles.account}>
          {isPending ? null : me ? (
            <>
              <NavLink
                to="/account"
                className={({ isActive }) =>
                  isActive ? styles.active : styles.link
                }
              >
                {me.displayName}
              </NavLink>
              <button
                type="button"
                className={styles.cta}
                disabled={logout.isPending}
                onClick={() =>
                  logout.mutate(undefined, { onSuccess: () => navigate("/") })
                }
              >
                {intl.formatMessage({ id: "common.logout" })}
              </button>
            </>
          ) : (
            <Link to="/login" className={styles.cta}>
              {intl.formatMessage({ id: "common.login" })}
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
