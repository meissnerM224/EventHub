import styles from "../components/EventCard/EventCard.module.css";

import EventCard from "../components/EventCard/EventCard";
import { useQuery } from "@tanstack/react-query";
import { getEvents } from "../api/events";
import Spinner from "../components/Spinner/Spinner.tsx";
import ErrorMessage from "../components/ErrorMessage/ErrorMessage.tsx";

export default function EventPage() {
  // Logic
  const {
    data: events,
    isPending,
    error,
  } = useQuery({
    queryKey: ["events"],
    queryFn: getEvents,
  });
  const refetch = async () => {
    setTimeout(() => {
      getEvents();
    }, 500);
  };
  // Component
  if (isPending) return <Spinner />;
  if (error) return <ErrorMessage message={error.message} onRetry={refetch} />;

  return (
    <main className={styles.list}>
      {events.map((event, index) => (
        <EventCard key={`${event.title}/${index}`} {...event} />
      ))}
    </main>
  );
}
