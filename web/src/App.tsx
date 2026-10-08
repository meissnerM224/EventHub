import EventPage from "./pages/EventPage.tsx";
import EventDetailsPage from "./pages/EventDetailsPage.tsx";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import Layout from "./components/Layout/Layout.tsx";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<EventPage />} />
          <Route path="/events/:id" element={<EventDetailsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
  // <main> {EventPage()}</main>
}
