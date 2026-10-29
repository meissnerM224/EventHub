import type React from "react";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { IntlProvider } from "react-intl";
import { MemoryRouter } from "react-router-dom";
import { messages } from "../i18n/locale";

export function renderPage(ui: React.ReactElement, { route = "/" } = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    queryClient,
    ...render(
      <QueryClientProvider client={queryClient}>
        <IntlProvider locale="de" messages={messages.de}>
          <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
        </IntlProvider>
      </QueryClientProvider>,
    ),
  };
}
