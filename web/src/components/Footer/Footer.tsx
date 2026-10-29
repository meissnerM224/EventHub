import styles from "./Footer.module.css";
import { useIntl } from "react-intl";
export default function Footer() {
  const intl = useIntl();
  return (
    <footer className={styles.footer}>
      <div className={styles.inner}>
        <p>EventHub – {intl.formatMessage({ id: "common.subtitle" })}</p>
      </div>
    </footer>
  );
}
