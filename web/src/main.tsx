import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App.tsx";
import { queryClient } from "./api/queryClient";
import { QueryClientProvider } from "@tanstack/react-query";
import { IntlProvider } from "react-intl";
import { detectLocale, messages } from "./i18n/locale";

const locale = detectLocale();
document.documentElement.lang = locale;

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <IntlProvider
      locale={locale}
      messages={messages[locale]}
      defaultLocale="de"
    >
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </IntlProvider>
  </StrictMode>,
);
