import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useIntl } from "react-intl";
import styles from "./CreateNewEvent.module.css";
import { useCategories } from "../../api/categories";
import { useCreateEvent } from "../../api/events";
import { errorMessageId } from "../../api/errors";
import { localInputToUtcIso } from "../../lib/datetime";
import Banner from "../../components/Banner/Banner";

export default function CreateNewEvent() {
  const intl = useIntl();
  const navigate = useNavigate();
  const {
    data: categories,
    isPending,
    isError,
    isFetching,
    refetch,
  } = useCategories();
  const createEvent = useCreateEvent();

  const [imageFile, setImageFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  // An object URL holds the file in memory until revoked.
  // Cleanup runs when the preview changes and when the page is left.
  useEffect(() => {
    if (!previewUrl) return;
    return () => URL.revokeObjectURL(previewUrl);
  }, [previewUrl]);

  function handleImageChange(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0] ?? null;
    setImageFile(file);
    setPreviewUrl(file ? URL.createObjectURL(file) : null);
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const text = (name: string) => String(form.get(name) ?? "").trim();

    createEvent.mutate(
        {
          event: {
            title: text("title"),
            description: text("description"),
            location: text("location"),
            startAt: localInputToUtcIso(text("startAt")),
            doorsOpenAt: localInputToUtcIso(text("doorsOpenAt")),
            maxParticipants: Number(form.get("maxParticipants")),
            categoryId: Number(form.get("categoryId")),
          },
          imageFile,
        },
        { onSuccess: (created) => navigate(`/events/${created.id}`) },
    );
  }

  return (
      <div className={styles.page}>
        <div className={styles.container}>
          <form className={styles.form} onSubmit={handleSubmit}>
            <div className={styles.header}>
              <h1>{intl.formatMessage({ id: "createEvent.title" })}</h1>
              <p>{intl.formatMessage({ id: "createEvent.subtitle" })}</p>
            </div>

            {isError && (
                <Banner
                    messageId="createEvent.category.loadError"
                    onRetry={refetch}
                    retrying={isFetching}
                />
            )}

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
                <select
                    name="categoryId"
                    required
                    defaultValue=""
                    disabled={isPending || isError}
                >
                  <option value="" disabled>
                    {intl.formatMessage({
                      id: isPending
                          ? "common.loading"
                          : "createEvent.category.placeholder",
                    })}
                  </option>
                  {categories?.map((category) => (
                      <option key={category.id} value={category.id}>
                        {category.name}
                      </option>
                  ))}
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
                    onChange={handleImageChange}
                />
                <small id="image-hint" className={styles.hint}>
                  {intl.formatMessage({ id: "createEvent.image.hint" })}
                </small>
              </label>
            </div>

            {previewUrl && (
                <img
                    src={previewUrl}
                    alt={intl.formatMessage({ id: "createEvent.image.preview" })}
                    className={styles.preview}
                />
            )}

            {createEvent.isError && (
                <Banner messageId={errorMessageId(createEvent.error)} />
            )}

            <div className={styles.actions}>
              <Link to="/" className={styles.secondary}>
                {intl.formatMessage({ id: "createEvent.cancel" })}
              </Link>
              <button
                  type="submit"
                  className={styles.submit}
                  disabled={createEvent.isPending}
              >
                {intl.formatMessage({
                  id: createEvent.isPending
                      ? "createEvent.submitting"
                      : "createEvent.submit",
                })}
              </button>
            </div>
          </form>
        </div>
      </div>
  );
}