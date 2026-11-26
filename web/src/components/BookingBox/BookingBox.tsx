import { Link, useLocation } from "react-router-dom";
import { useIntl } from "react-intl";
import { useMe } from "../../api/auth";
import {
  useBookEvent,
  useCancelBooking,
  useMyBookings,
} from "../../api/bookings";
import { errorMessageId } from "../../api/errors";
import type { EventDetail } from "../../api/types";
import type { MessageId } from "../../i18n/locale";
import styles from "./BookingBox.module.css";

type Props = {
  event: Pick<EventDetail, "eventId" | "availableSpots" | "isCancelled">;
};

export default function BookingBox({ event }: Props) {
  const intl = useIntl();
  const location = useLocation();
  const { data: user, isPending: mePending } = useMe();
  const { data: bookings, isPending: bookingsPending } = useMyBookings();
  const book = useBookEvent();
  const cancel = useCancelBooking();

  // All hooks above this line – no hook after an early return.
  const t = (id: MessageId) => intl.formatMessage({ id });

  if (event.isCancelled) {
    return (
      <div className={`${styles.box} ${styles.warning}`}>
        <p className={styles.text}>{t("error.event.cancelled")}</p>
      </div>
    );
  }

  if (mePending) return null;

  // 1) not logged in
  if (!user) {
    return (
      <div className={styles.box}>
        <Link
          to="/login"
          state={{ from: location.pathname }}
          className={styles.button}
        >
          {t("booking.loginToBook")}
        </Link>
      </div>
    );
  }

  // Keep the box empty until we know whether the user booked – otherwise
  // "Buchen" flashes up and turns into "Gebucht" a moment later.
  if (bookingsPending) return null;

  const isBooked = bookings?.some((b) => b.eventId === event.eventId) ?? false;
  const error = book.error ?? cancel.error;
  const busy = book.isPending || cancel.isPending;

  // 2) already booked
  if (isBooked) {
    return (
      <div className={`${styles.box} ${styles.success}`}>
        <p className={styles.text}>{t("booking.booked")}</p>
        <button
          type="button"
          className={styles.secondary}
          disabled={busy}
          onClick={() => {
            book.reset();
            cancel.mutate(event.eventId);
          }}
        >
          {t(cancel.isPending ? "booking.cancelling" : "booking.cancel")}
        </button>
        {error && <ErrorText error={error} />}
      </div>
    );
  }

  // 3) sold out
  if (event.availableSpots <= 0) {
    return (
      <div className={`${styles.box} ${styles.warning}`}>
        <p className={styles.text}>{t("event.soldOut")}</p>
      </div>
    );
  }

  // 4) bookable
  return (
    <div className={styles.box}>
      <button
        type="button"
        className={styles.button}
        disabled={busy}
        onClick={() => {
          cancel.reset();
          book.mutate(event.eventId);
        }}
      >
        {t(book.isPending ? "booking.booking" : "event.booking")}
      </button>
      {error && <ErrorText error={error} />}
    </div>
  );
}

function ErrorText({ error }: { error: unknown }) {
  const intl = useIntl();
  return (
    <p className={styles.error} role="alert">
      {intl.formatMessage({ id: errorMessageId(error) })}
    </p>
  );
}
