import EventPage from "./pages/EventPage/EventPage";
import EventDetailsPage from "./pages/EventDetailsPage/EventDetailsPage";
import RequireAuth from "./components/RequireAuth/RequireAuth.tsx";
import LoginPage from "./pages/LoginPage/LoginPage";
import AccountLayout from "./components/Layout/AccountLayout";
import Layout from "./components/Layout/Layout";
import RegisterPage from "./pages/RegisterPage/RegisterPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage/ForgotPasswordPage";
import SettingsPage from "./pages/Account/SettingsPage";
import MyBookingsPage from "./pages/Account/MyBookingsPage";
import CreateNewEvent from "./pages/CreateNewEvent/CreateNewEvent";
import NotFoundPage from "./pages/NotFoundPage/NotFoundPage";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<EventPage />} />
          <Route path="/events/:id" element={<EventDetailsPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route
            path="/events/new"
            element={
              <RequireAuth>
                <Route path="settings" element={<CreateNewEvent />} />
              </RequireAuth>
            }
          >
            {" "}
          </Route>
          <Route
            path="/account"
            element={
              <RequireAuth>
                <AccountLayout />
              </RequireAuth>
            }
          >
            <Route index element={<Navigate to="bookings" replace />} />
            <Route path="bookings" element={<MyBookingsPage />} />
            <Route path="settings" element={<SettingsPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
