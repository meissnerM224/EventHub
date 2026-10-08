import styles from "./EventCard.module.css";
import { formatDate } from "../../lib/format.ts";
import { Link } from "react-router-dom";

type EventCardProps = {
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
  return (
    <Link to={`/events/${id}`}>
      <div className={styles.container}>
        {imageUrl && <img src={imageUrl} alt="" />}
        <div className={styles.body}>
          <h3>{title}</h3>
          <p>Datum: {formatDate(startsAt)}</p>
          <p>Ort: {location}</p>
          <p> Verfügbare Plätze: {availableSpots}</p>
          <p>Kategorie: {categoryName}</p>

          <button disabled={availableSpots <= 0}>
            {availableSpots > 0 ? "Booking now" : "Sold out"}
          </button>
        </div>
      </div>
    </Link>
  );
}
