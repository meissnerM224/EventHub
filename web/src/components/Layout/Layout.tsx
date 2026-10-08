import { Outlet } from "react-router-dom";
import styles from "./Layout.module.css";
import Header from "../Header/Header.tsx";
import Footer from "../Footer/Footer.tsx";

export default function Layout() {
  return (
    <div className={styles.page}>
      <Header />
      <main className={`${styles.container} ${styles.content}`}>
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}
