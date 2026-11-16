import style from "./ErrorMessage.module.css";
import { useIntl } from "react-intl";
import { errorMessageId } from "../../api/errors.ts";
type Props = {
  error: unknown;
  onRetry: () => void;
  retrying?: boolean;
};

export default function ErrorMessage({ error, onRetry, retrying }: Props) {
  const intl = useIntl();
  return (
    <div className={style.center}>
      <div className={style.wrapper} role="alert">
        <p className={style.message}>
          {intl.formatMessage({ id: errorMessageId(error) })}
        </p>
        {onRetry && (
          <button
            type="button"
            className={style.retry}
            onClick={onRetry}
            disabled={retrying}
          >
            {intl.formatMessage({ id: "common.retry" })}
          </button>
        )}
      </div>
    </div>
  );
}
