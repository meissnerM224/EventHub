import { useParams } from "react-router-dom";
import styles from "../components/EventCard/EventCard.module.css";
import { formatDate } from "../lib/format.ts";
import { useQuery } from "@tanstack/react-query";
import { getEventById } from "../api/events.ts";
import Spinner from "../components/Spinner/Spinner.tsx";
import ErrorMessage from "../components/ErrorMessage/ErrorMessage.tsx";

function EventDetailsPage() {
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

  if (!event) return <p>Event nicht gefunden.</p>;
  return (
    <div className={styles.container}>
      {event.eventImageUrl && <img src={event.eventImageUrl} alt="" />}
      <div className={styles.body}>
        <h3>{event.eventTitle}</h3>
        <p>Datum: {formatDate(event.eventStartsAt)}</p>
        <p>Ort: {event.eventLocation}</p>
        <p> Verfügbare Plätze: {event.availableSpots}</p>
        <p>Kategorie: {event.categoryName}</p>
        <button disabled={event.availableSpots <= 0}>
          {event.availableSpots > 0 ? "Booking now" : "Sold out"}
        </button>
      </div>
    </div>
  );
}

export default EventDetailsPage;
