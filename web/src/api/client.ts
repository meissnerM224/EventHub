// ------------------ CRUD ------------------ \\

/// GET
export async function apiGet<T>(
  path: string,
  parse: (data: unknown) => T,
): Promise<T> {
  return parseJson(await apiFetch(path), parse);
}

/// POST
export async function apiPost<T>(
  path: string,
  data: unknown,
  parse: (data: unknown) => T,
): Promise<T> {
  const response = await apiFetch(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  return parseJson(response, parse);
}

export async function apiFetch(
  path: string,
  init?: RequestInit,
): Promise<Response> {
  const response = await fetch(path, init);
  if (!response.ok) {
    const { message, code } = await readProblem(response);
    throw new ApiError(response.status, message, code);
  }
  return response;
}

// ------------------ API Error ------------------ \\
export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(status: number, message: string, code?: string) {
    super(message);
    this.status = status;
    this.name = "ApiError";
    this.code = code;
  }
}

// Helper
export async function parseJson<T>(
  response: Response,
  parse: (data: unknown) => T,
): Promise<T> {
  let data: unknown;
  try {
    data = await response.json();
  } catch (cause) {
    throw new Error("Response body is not valid JSON.", { cause });
  }
  try {
    return parse(data);
  } catch (cause) {
    console.error("Response does not fit schema", cause);
    throw new Error("Response does not match expected schema.", { cause });
  }
}

const fallbackMessages: Record<number, string> = {
  401: "Please sign in",
  403: "You don't have access to this",
  404: "Not Found",
  500: "Internal Server Error",
};

async function readProblem(
  response: Response,
): Promise<{ message: string; code?: string }> {
  const fallback = fallbackMessages[response.status] ?? "Unexpected error";
  try {
    const problem = await response.json();
    const message =
      typeof problem?.detail === "string"
        ? problem.detail
        : typeof problem?.title === "string"
          ? problem.title
          : fallback;
    const code = typeof problem?.code === "string" ? problem.code : undefined;
    return { message, code };
  } catch {
    return { message: fallback };
  }
}
