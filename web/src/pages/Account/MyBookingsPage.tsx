import { useState } from "react";
import { Link } from "react-router-dom";
import { useIntl } from "react-intl";
import { useCancelBooking, useMyBookings } from "../../api/bookings";
import { errorMessageId } from "../../api/errors";
import { formatDate } from "../../lib/format";
import { hasStarted } from "../../lib/eventTime";
import Spinner from "../../components/Spinner/Spinner";
import Banner from "../../components/Banner/Banner";
import styles from "./Account.module.css";

type Tab = "upcoming" | "past";
const TABS: Tab[] = ["upcoming", "past"];

export default function MyBookingsPage() {
  const intl = useIntl();
  const [tab, setTab] = useState<Tab>("upcoming");
  const {
    data: bookings,
    isPending,
    isError,
    error,
    isFetching,
    refetch,
  } = useMyBookings();
  const cancel = useCancelBooking();

  if (isPending) return <Spinner />;

  const visible =
    bookings?.filter((b) =>
      tab === "past"
        ? hasStarted(b.eventStartsAt)
        : !hasStarted(b.eventStartsAt),
    ) ?? [];

  return (
    <div className={styles.container}>
      <div className={styles.body}>
        <h2>{intl.formatMessage({ id: "account.nav.bookings" })}</h2>

        {isError && (
          <Banner
            messageId={errorMessageId(error)}
            onRetry={refetch}
            retrying={isFetching}
          />
        )}
        {cancel.isError && <Banner messageId={errorMessageId(cancel.error)} />}

        {bookings?.length === 0 && (
          <>
            <p className={styles.muted}>
              {intl.formatMessage({ id: "account.bookings.empty" })}
            </p>
            <Link to="/" className={styles.cta}>
              {intl.formatMessage({ id: "account.bookings.browse" })}
            </Link>
          </>
        )}

        {bookings && bookings.length > 0 && (
          <>
            <div className={styles.subTabs} role="tablist">
              {TABS.map((t) => (
                <button
                  key={t}
                  type="button"
                  role="tab"
                  aria-selected={tab === t}
                  className={tab === t ? styles.subTabActive : styles.subTab}
                  onClick={() => setTab(t)}
                >
                  {intl.formatMessage({ id: `account.bookings.${t}` })}
                </button>
              ))}
            </div>

            {visible.length === 0 ? (
              <p className={styles.muted}>
                {intl.formatMessage({
                  id:
                    tab === "past"
                      ? "account.bookings.emptyPast"
                      : "account.bookings.empty",
                })}
              </p>
            ) : (
              <ul className={styles.bookings} role="tabpanel">
                {visible.map((booking) => {
                  // One mutation for all rows: variables tells which row is running.
                  const cancelling =
                    cancel.isPending && cancel.variables === booking.eventId;
                  return (
                    <li key={booking.bookingId} className={styles.booking}>
                      <div className={styles.bookingInfo}>
                        <Link to={`/events/${booking.eventId}`}>
                          {booking.eventTitle}
                        </Link>
                        <span className={styles.muted}>
                          {formatDate(booking.eventStartsAt, intl.locale)}
                        </span>
                        {booking.isCancelled && (
                          <span className={styles.warning}>
                            {intl.formatMessage({
                              id: "error.event.cancelled",
                            })}
                          </span>
                        )}
                      </div>
                      {tab === "upcoming" && (
                        <button
                          type="button"
                          className={styles.secondary}
                          disabled={cancel.isPending}
                          onClick={() => cancel.mutate(booking.eventId)}
                        >
                          {intl.formatMessage({
                            id: cancelling
                              ? "booking.cancelling"
                              : "booking.cancel",
                          })}
                        </button>
                      )}
                    </li>
                  );
                })}
              </ul>
            )}
          </>
        )}
      </div>
    </div>
  );
}
