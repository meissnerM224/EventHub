import styles from "./EventPage.module.css";
import EmptyState from "../../components/EmptyState/EmptyState";
import EventCard from "../../components/EventCard/EventCard";
import { useQuery } from "@tanstack/react-query";
import { getEvents } from "../../api/events";
import Spinner from "../../components/Spinner/Spinner.tsx";
import ErrorMessage from "../../components/ErrorMessage/ErrorMessage.tsx";
import { useIntl } from "react-intl";
export default function EventPage() {
  // Logic
  const {
    data: events,
    isPending,
    error,
    refetch,
    isFetching,
  } = useQuery({
    queryKey: ["events"],
    queryFn: getEvents,
  });
  const intl = useIntl();

  // Component
  if (isPending || isFetching)
    return <Spinner label={intl.formatMessage({ id: "common.loading" })} />;
  if (error) return <ErrorMessage message={error.message} onRetry={refetch} />;
  if (events.length === 0)
    return (
      <EmptyState
        message={intl.formatMessage({ id: "events.empty.title" })}
        action={{
          label: intl.formatMessage({
            id: isFetching ? "common.loading" : "common.refresh",
          }),
          onClick: () => refetch(),
          disabled: isFetching,
        }}
      />
    );
  return (
    <main className={styles.list}>
      {events.map((event, index) => (
        <EventCard key={`${event.title}/${index}`} {...event} />
      ))}
    </main>
  );
}
