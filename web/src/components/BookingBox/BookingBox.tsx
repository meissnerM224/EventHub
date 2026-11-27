import { useLocation } from "react-router-dom";
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
import { hasStarted } from "../../lib/eventTime";
import styles from "./BookingBox.module.css";
import { Button, ButtonLink } from "../Button/Button";

type Props = {
  event: Pick<
    EventDetail,
    "eventId" | "availableSpots" | "isCancelled" | "eventStartsAt"
  >;
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

  // Same rule as MyBookingsPage and the backend (event.already_started):
  // from the start on, neither booking nor cancelling is possible.
  if (hasStarted(event.eventStartsAt)) {
    return (
      <div className={styles.box}>
        <p className={styles.text}>{t("error.event.already_started")}</p>
      </div>
    );
  }

  if (mePending) return null;

  // 1) not logged in
  if (!user) {
    return (
      <div className={styles.box}>
        <ButtonLink to="/login" state={{ from: location.pathname }}>
          {t("booking.loginToBook")}
        </ButtonLink>
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
        <Button
          variant="secondary"
          className={styles.push}
          disabled={busy}
          onClick={() => {
            book.reset();
            cancel.mutate(event.eventId);
          }}
        >
          {t(cancel.isPending ? "booking.cancelling" : "booking.cancel")}
        </Button>
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
      <Button
        disabled={busy}
        onClick={() => {
          cancel.reset();
          book.mutate(event.eventId);
        }}
      >
        {t(book.isPending ? "booking.booking" : "event.booking")}
      </Button>
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
