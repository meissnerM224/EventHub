import styles from "./Spinner.module.css";
import { useIntl } from "react-intl";

export default function Spinner({ label }: { label?: string }) {
  const intl = useIntl();

  return (
    <div className={styles.center}>
      <div className={styles.wrapper} role="status" aria-live="polite">
        <div className={styles.circle} aria-hidden="true" />
        <span className={styles.label}>
          {label ?? intl.formatMessage({ id: "common.loading" })}
        </span>
      </div>
    </div>
  );
}
