import { useIntl } from "react-intl";
import { useMe } from "../../api/auth";
import styles from "./Account.module.css";

export default function SettingsPage() {
  const intl = useIntl();
  const { data: me } = useMe();

  return (
    <div className={styles.container}>
      <div className={styles.body}>
        <h2>{intl.formatMessage({ id: "account.settings.profile" })}</h2>
        <dl className={styles.list}>
          <dt>{intl.formatMessage({ id: "auth.displayName" })}</dt>
          <dd>{me?.displayName}</dd>
          <dt>{intl.formatMessage({ id: "account.settings.roles" })}</dt>
          <dd>{me?.roles.length ? me.roles.join(", ") : "–"}</dd>
        </dl>
      </div>
    </div>
  );
}
