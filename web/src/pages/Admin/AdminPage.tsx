import type { FormEvent } from "react";
import { useIntl } from "react-intl";
import { Role, hasRole } from "../../api/auth";
import {
  useAdminUsers,
  useCreateCategory,
  useSetOrganizer,
} from "../../api/admin";
import { useCategories } from "../../api/categories";
import { errorMessageId } from "../../api/errors";
import Banner from "../../components/Banner/Banner";
import Spinner from "../../components/Spinner/Spinner";
import styles from "./AdminPage.module.css";

export default function AdminPage() {
  const intl = useIntl();
  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1>{intl.formatMessage({ id: "admin.title" })}</h1>
      </div>
      <UsersCard />
      <CategoriesCard />
    </div>
  );
}

function UsersCard() {
  const intl = useIntl();
  const {
    data: users,
    isPending,
    isError,
    error,
    isFetching,
    refetch,
  } = useAdminUsers();
  const setOrganizer = useSetOrganizer();

  return (
    <section className={styles.card}>
      <h2>{intl.formatMessage({ id: "admin.users.title" })}</h2>

      {isPending && <Spinner />}
      {isError && (
        <Banner
          messageId={errorMessageId(error)}
          onRetry={refetch}
          retrying={isFetching}
        />
      )}
      {setOrganizer.isError && (
        <Banner messageId={errorMessageId(setOrganizer.error)} />
      )}

      {users && (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{intl.formatMessage({ id: "auth.displayName" })}</th>
              <th>{intl.formatMessage({ id: "auth.email" })}</th>
              <th>{intl.formatMessage({ id: "account.settings.roles" })}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {users.map((user) => {
              const organizer = hasRole(user, Role.Organizer);
              const busy =
                setOrganizer.isPending &&
                setOrganizer.variables?.userId === user.userId;
              return (
                <tr key={user.userId}>
                  <td>{user.displayName}</td>
                  <td>{user.email}</td>
                  <td className={styles.muted}>{user.roles.join(", ")}</td>
                  <td className={styles.actionCell}>
                    <button
                      type="button"
                      className={organizer ? styles.secondary : styles.primary}
                      disabled={setOrganizer.isPending}
                      onClick={() =>
                        setOrganizer.mutate({
                          userId: user.userId,
                          grant: !organizer,
                        })
                      }
                    >
                      {intl.formatMessage({
                        id: busy
                          ? "common.loading"
                          : organizer
                            ? "admin.users.revokeOrganizer"
                            : "admin.users.grantOrganizer",
                      })}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
}

function CategoriesCard() {
  const intl = useIntl();
  const { data: categories } = useCategories();
  const createCategory = useCreateCategory();

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formElement = e.currentTarget;
    const form = new FormData(formElement);
    createCategory.mutate(
      {
        name: String(form.get("name") ?? "").trim(),
        description: String(form.get("description") ?? "").trim(),
      },
      { onSuccess: () => formElement.reset() },
    );
  }

  return (
    <section className={styles.card}>
      <h2>{intl.formatMessage({ id: "admin.categories.title" })}</h2>

      {categories && (
        <ul className={styles.chips}>
          {categories.map((category) => (
            <li key={category.id} className={styles.chip}>
              {category.name}
            </li>
          ))}
        </ul>
      )}

      <form className={styles.form} onSubmit={handleSubmit}>
        <label className={styles.field}>
          {intl.formatMessage({ id: "admin.categories.name" })}
          <input name="name" required maxLength={100} />
        </label>
        <label className={styles.field}>
          {intl.formatMessage({ id: "createEvent.description" })}
          <input name="description" maxLength={400} />
        </label>
        <button
          type="submit"
          className={styles.primary}
          disabled={createCategory.isPending}
        >
          {intl.formatMessage({ id: "admin.categories.submit" })}
        </button>
      </form>

      {createCategory.isError && (
        <Banner messageId={errorMessageId(createCategory.error)} />
      )}
    </section>
  );
}
