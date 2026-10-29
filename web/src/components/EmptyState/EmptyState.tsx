import styles from "./EmptyState.module.css";

type Props = {
  message: string;
  action?: { label: string; onClick: () => void; disabled?: boolean };
};
export default function EmptyState({ message, action }: Props) {
  return (
    <div className={styles.center}>
      <div className={styles.body}>
        <h2>{message}</h2>
        {action && (
          <button
            type="button"
            className={styles.retry}
            onClick={action.onClick}
            disabled={action.disabled}
          >
            {action.label}
          </button>
        )}
      </div>
    </div>
  );
}
