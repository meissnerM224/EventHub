import { ButtonLink } from "../../components/Button/Button";
import { useIntl } from "react-intl";
import styles from "./NotFoundPage.module.css";

export default function NotFoundPage() {
  const intl = useIntl();

  return (
    <div className={styles.center}>
      <div className={styles.container}>
        <div className={styles.body}>
          <h1 className={styles.code}>404</h1>
          <h2>{intl.formatMessage({ id: "notFound.title" })}</h2>
          <p className={styles.message}>
            {intl.formatMessage({ id: "notFound.message" })}
          </p>
          <ButtonLink to="/">
            {intl.formatMessage({ id: "notFound.back" })}
          </ButtonLink>
        </div>
      </div>
    </div>
  );
}
