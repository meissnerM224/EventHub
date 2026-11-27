import { useIntl } from "react-intl";
import type { MessageId } from "../../i18n/locale";
import styles from "./Banner.module.css";
import { Button } from "../Button/Button";

type Props = {
  messageId: MessageId;
  onRetry?: () => void;
  retrying?: boolean;
};

// Small inline error bar – for errors that don't replace the whole page.
export default function Banner({ messageId, onRetry, retrying }: Props) {
  const intl = useIntl();
  return (
    <div className={styles.banner} role="alert">
      <p className={styles.message}>{intl.formatMessage({ id: messageId })}</p>
      {onRetry && (
        <Button
          variant="warning"
          size="sm"
          className={styles.retry}
          onClick={() => onRetry()}
          disabled={retrying}
        >
          {intl.formatMessage({ id: "common.retry" })}
        </Button>
      )}
    </div>
  );
}
