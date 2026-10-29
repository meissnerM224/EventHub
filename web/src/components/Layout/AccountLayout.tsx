import { NavLink, Outlet } from "react-router-dom";

export default function AccountLayout() {
  return (
    <main>
      <nav>
        <NavLink to="bookings">Buchungen</NavLink>
        <NavLink to="settings">Einstellungen</NavLink>
      </nav>
      <Outlet />
    </main>
  );
}
