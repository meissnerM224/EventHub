import { useParams } from "react-router-dom";
import styles from "../components/EventCard/EventCard.module.css";
import { formatDate } from "../lib/format.ts";
import { useQuery } from "@tanstack/react-query";
import { getEventById } from "../api/events.ts";
import Spinner from "../components/Spinner/Spinner.tsx";
import ErrorMessage from "../components/ErrorMessage/ErrorMessage.tsx";
import { useIntl } from "react-intl";
function EventDetailsPage() {
  const intl = useIntl();
  const { id } = useParams();
  // Logic
  const {
    data: event,
    isPending,
    error,
  } = useQuery({
    queryKey: ["events", id],
    queryFn: () => getEventById(id!),
    enabled: !!id,
  });

  if (isPending) return <Spinner />;
  if (error) return <ErrorMessage message={error.message} onRetry={() => {}} />;

  if (!event) return <p>{intl.formatMessage({ id: "events.notFound" })}</p>;
  return (
    <div className={styles.container}>
      {event.eventImageUrl && <img src={event.eventImageUrl} alt="" />}
      <div className={styles.body}>
        <h3>{event.eventTitle}</h3>
        <p>
          {intl.formatMessage({ id: "date" })}:{" "}
          {formatDate(event.eventStartsAt)}
        </p>
        <p>
          {intl.formatMessage({ id: "location" })}: {event.eventLocation}
        </p>
        <p>
          {" "}
          {intl.formatMessage({ id: "event.availanle.spots" })}:{" "}
          {event.availableSpots}
        </p>
        <p>
          {intl.formatMessage({ id: "events.categorie" })}: {event.categoryName}
        </p>
        <button disabled={event.availableSpots <= 0}>
          {intl.formatMessage({
            id: event.availableSpots > 0 ? "event.booking" : "event.soultout",
          })}
        </button>
      </div>
    </div>
  );
}

export default EventDetailsPage;
