import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useIntl } from "react-intl";
import styles from "./CreateNewEvent.module.css";
import { useCategories } from "../../api/categories";
import { useCreateEvent } from "../../api/events";
import { errorMessageId } from "../../api/errors";
import { localInputToUtcIso } from "../../lib/datetime";
import Banner from "../../components/Banner/Banner";
import { Button, ButtonLink } from "../../components/Button/Button";
import {
  SelectField,
  TextAreaField,
  TextField,
} from "../../components/Field/Field";

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

          <TextField
            label={intl.formatMessage({ id: "createEvent.eventTitle" })}
            name="title"
            required
            maxLength={200}
          />

          <TextAreaField
            label={intl.formatMessage({ id: "createEvent.description" })}
            name="description"
            required
            maxLength={500}
          />

          <div className={styles.row}>
            <TextField
              label={intl.formatMessage({ id: "createEvent.location" })}
              name="location"
              required
            />
            <SelectField
              label={intl.formatMessage({ id: "createEvent.category" })}
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
            </SelectField>
          </div>

          <div className={styles.row}>
            <TextField
              label={intl.formatMessage({ id: "createEvent.startsAt" })}
              name="startAt"
              type="datetime-local"
              required
            />
            <TextField
              label={intl.formatMessage({ id: "createEvent.doorsOpenAt" })}
              name="doorsOpenAt"
              type="datetime-local"
              required
            />
          </div>

          <div className={styles.row}>
            <TextField
              label={intl.formatMessage({ id: "createEvent.maxParticipants" })}
              name="maxParticipants"
              type="number"
              min={1}
              required
            />
            <TextField
              label={intl.formatMessage({ id: "createEvent.image" })}
              hint={intl.formatMessage({ id: "createEvent.image.hint" })}
              name="image"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={handleImageChange}
            />
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
            <ButtonLink to="/" variant="secondary">
              {intl.formatMessage({ id: "createEvent.cancel" })}
            </ButtonLink>
            <Button type="submit" disabled={createEvent.isPending}>
              {intl.formatMessage({
                id: createEvent.isPending
                  ? "createEvent.submitting"
                  : "createEvent.submit",
              })}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
