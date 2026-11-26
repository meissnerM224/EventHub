import styles from "./EmptyState.module.css";
import { Button } from "../Button/Button";

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
          <Button onClick={action.onClick} disabled={action.disabled}>
            {action.label}
          </Button>
        )}
      </div>
    </div>
  );
}
