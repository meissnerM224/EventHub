export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = "ApiError";
  }
}

const fallbackMessages: Record<number, string> = {
  401: "Please sign in",
  403: "You don't have access to this",
  404: "Not Found",
  500: "Internal Server Error",
};

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const problem = await response.json();
    if (typeof problem?.detail === "string") return problem.detail;
    if (typeof problem?.title === "string") return problem.title;
  } catch {
    /* empty */
  }
  return fallbackMessages[response.status] ?? "unexpected error";
}

export async function apiFetch(
  path: string,
  init?: RequestInit,
): Promise<Response> {
  const response = await fetch(path, init);
  if (!response.ok) {
    throw new ApiError(response.status, await readErrorMessage(response));
  }
  return response;
}

export async function apiGet<T>(
  path: string,
  parse: (data: unknown) => T,
): Promise<T> {
  const response = await apiFetch(path);
  try {
    return parse(await response.json());
  } catch (cause) {
    console.error("Response does not fit in schema", cause);
    throw new Error("Server sent an unexpected schema.", { cause });
  }
}
