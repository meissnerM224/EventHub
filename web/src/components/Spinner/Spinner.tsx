import styles from "./Spinner.module.css";

export default function Spinner({ label = "Lädt …" }: { label?: string }) {
  return (
    <div className={styles.center}>
      <div className={styles.wrapper} role="status" aria-live="polite">
        <div className={styles.circle} aria-hidden="true" />
        <span className={styles.label}>{label}</span>
      </div>
    </div>
  );
}
