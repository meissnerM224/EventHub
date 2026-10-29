import { setupServer } from "msw/node";

// Handlers are registered per test via server.use(...)
export const server = setupServer();
