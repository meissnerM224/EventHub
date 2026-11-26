import { useParams } from "react-router-dom";
import styles from "./EventDetailsPage.module.css";
import { formatDate } from "../../lib/format.ts";
import { useQuery } from "@tanstack/react-query";
import { getEventById } from "../../api/events.ts";
import Spinner from "../../components/Spinner/Spinner.tsx";
import ErrorMessage from "../../components/ErrorMessage/ErrorMessage.tsx";
import { useIntl } from "react-intl";
import BookingBox from "../../components/BookingBox/BookingBox.tsx";

function EventDetailsPage() {
  const intl = useIntl();
  const { id } = useParams();
  // Logic
  const {
    data: event,
    isPending,
    error,
    isFetching,
    refetch,
  } = useQuery({
    queryKey: ["events", id],
    queryFn: () => getEventById(id!),
    enabled: !!id,
  });

  if (isPending) return <Spinner />;
  if (error)
    return (
      <ErrorMessage error={error} onRetry={refetch} retrying={isFetching} />
    );

  if (!event) return <p>{intl.formatMessage({ id: "event.notFound" })}</p>;
  return (
    <div className={styles.container}>
      {event.eventImageUrl && (
        <img src={event.eventImageUrl} alt="Event image" />
      )}
      <div className={styles.body}>
        <h3>{event.eventTitle}</h3>
        <p>
          {intl.formatMessage({ id: "common.date" })}:{" "}
          {formatDate(event.eventStartsAt, intl.locale)}
        </p>
        <p>
          {intl.formatMessage({ id: "common.location" })}: {event.eventLocation}
        </p>
        <p>
          {intl.formatMessage({ id: "event.availableSpots" })}:{" "}
          {event.availableSpots}
        </p>
        <p>
          {intl.formatMessage({ id: "event.category" })}: {event.categoryName}
        </p>
        <BookingBox event={event} />
      </div>
    </div>
  );
}

export default EventDetailsPage;
