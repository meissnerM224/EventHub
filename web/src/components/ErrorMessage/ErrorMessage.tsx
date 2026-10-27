import style from "./ErrorMessage.module.css";
import { useIntl } from "react-intl";

type Props = {
  message: string;
  onRetry: () => void;
};

export default function ErrorMessage({ message, onRetry }: Props) {
  const intl = useIntl();
  return (
    <div className={style.center}>
      <div className={style.wrapper} role="alert">
        <p className={style.message}>{message}</p>
        {onRetry && (
          <button type="button" className={style.retry} onClick={onRetry}>
            {intl.formatMessage({ id: "common.retry" })}
          </button>
        )}
      </div>
    </div>
  );
}
