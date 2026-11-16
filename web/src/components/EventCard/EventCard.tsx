import styles from "./EventCard.module.css";
import { formatDate } from "../../lib/format.ts";
import { Link } from "react-router-dom";
import { useIntl } from "react-intl";

export type EventCardProps = {
  id: string;
  title: string;
  location: string;
  categoryName: string;
  startsAt: string;
  availableSpots: number;
  imageUrl: string | null;
};

export default function EventCard({
  id,
  title,
  location,
  categoryName,
  startsAt,
  availableSpots,
  imageUrl,
}: EventCardProps) {
  const intl = useIntl();
  const soldOut = availableSpots <= 0;

  return (
    <Link to={`/events/${id}`} className={styles.link}>
      <div className={styles.container}>
        {imageUrl && <img src={imageUrl} alt="" />}
        <div className={styles.body}>
          <h3>{title}</h3>
          <p>
            {intl.formatMessage({ id: "common.date" })}:{" "}
            {formatDate(startsAt, intl.locale)}
          </p>
          <p>
            {intl.formatMessage({ id: "common.location" })}: {location}
          </p>
          <p>
            {intl.formatMessage({ id: "event.availableSpots" })}:{" "}
            {availableSpots}
          </p>
          <p>
            {intl.formatMessage({ id: "event.category" })}: {categoryName}
          </p>
          {/* A link must not contain a button: booking happens on the detail page. */}
          <span className={soldOut ? styles.soldOut : styles.cta}>
            {intl.formatMessage({
              id: soldOut ? "event.soldOut" : "event.booking",
            })}
          </span>
        </div>
      </div>
    </Link>
  );
}
