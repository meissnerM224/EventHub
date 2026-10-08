import style from "./ErrorMessage.module.css";

type Props = {
  message: string;
  onRetry: () => void;
};

export default function ErrorMessage({ message, onRetry }: Props) {
  return (
    <div className={style.center}>
      <div className={style.wrapper} role="alert">
        <p className={style.message}>{message}</p>
        {onRetry && (
          <button type="button" className={style.retry} onClick={onRetry}>
            Erneut versuchen
          </button>
        )}
      </div>
    </div>
  );
}
