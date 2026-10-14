import styles from "./Footer.module.css";
export default function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={styles.inner}>
        <p>EventHub – ein Lernprojekt</p>
      </div>
    </footer>
  );
}
