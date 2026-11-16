import type { FormEvent } from "react";
import { Link } from "react-router-dom";
import { useIntl } from "react-intl";
import styles from "./CreateNewEvent.module.css";

export default function CreateNewEvent() {
  const intl = useIntl();

  // TODO: submit logic
  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
  }

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <form className={styles.form} onSubmit={handleSubmit}>
          <div className={styles.header}>
            <h1>{intl.formatMessage({ id: "createEvent.title" })}</h1>
            <p>{intl.formatMessage({ id: "createEvent.subtitle" })}</p>
          </div>

          <label className={styles.field}>
            {intl.formatMessage({ id: "createEvent.eventTitle" })}
            <input name="title" required maxLength={200} />
          </label>

          <label className={styles.field}>
            {intl.formatMessage({ id: "createEvent.description" })}
            <textarea name="description" required maxLength={500} />
          </label>

          <div className={styles.row}>
            <label className={styles.field}>
              {intl.formatMessage({ id: "createEvent.location" })}
              <input name="location" required />
            </label>
            <label className={styles.field}>
              {intl.formatMessage({ id: "createEvent.category" })}
              <select name="categoryId" required defaultValue="">
                <option value="" disabled>
                  {intl.formatMessage({
                    id: "createEvent.category.placeholder",
                  })}
                </option>
              </select>
            </label>
          </div>

          <div className={styles.row}>
            <label className={styles.field}>
              {intl.formatMessage({ id: "createEvent.startsAt" })}
              <input name="startAt" type="datetime-local" required />
            </label>
            <label className={styles.field}>
              {intl.formatMessage({ id: "createEvent.doorsOpenAt" })}
              <input name="doorsOpenAt" type="datetime-local" required />
            </label>
          </div>

          <div className={styles.row}>
            <label className={styles.field}>
              {intl.formatMessage({ id: "createEvent.maxParticipants" })}
              <input name="maxParticipants" type="number" min={1} required />
            </label>
            <label className={styles.field}>
              {intl.formatMessage({ id: "createEvent.image" })}
              <input
                name="image"
                type="file"
                accept="image/jpeg,image/png,image/webp"
                aria-describedby="image-hint"
              />
              <small id="image-hint" className={styles.hint}>
                {intl.formatMessage({ id: "createEvent.image.hint" })}
              </small>
            </label>
          </div>

          <div className={styles.actions}>
            <Link to="/" className={styles.secondary}>
              {intl.formatMessage({ id: "createEvent.cancel" })}
            </Link>
            <button type="submit" className={styles.submit}>
              {intl.formatMessage({ id: "createEvent.submit" })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
