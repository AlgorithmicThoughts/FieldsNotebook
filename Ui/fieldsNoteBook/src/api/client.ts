import { readAuthSession } from "@/state/authCookie";
import { ApiError } from "./errors";
import type { RequestOptions } from "./types";

/**
 * Artificial latency ceiling in milliseconds. Each call waits a fraction of it
 * so loading states are visible during development. Set to 0 to remove the
 * wait entirely.
 */
export const MOCK_LATENCY_MS = 400;

/** Share of mock calls that fail with a transient error. Set to 0 to disable. */
export const MOCK_FAILURE_RATE = 0;

function abortError(): DOMException {
  return new DOMException("Aborted", "AbortError");
}

function randomLatencyMs(): number {
  return Math.round(MOCK_LATENCY_MS * (0.4 + Math.random() * 0.6));
}

/** Resolves after the mock latency, or rejects if the caller aborts first. */
function waitForMockLatency(signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(abortError());
      return;
    }

    const onAbort = () => {
      clearTimeout(timer);
      reject(abortError());
    };

    const timer = setTimeout(() => {
      signal?.removeEventListener("abort", onAbort);
      resolve();
    }, randomLatencyMs());

    signal?.addEventListener("abort", onAbort, { once: true });
  });
}

function failTransiently(): void {
  if (Math.random() >= MOCK_FAILURE_RATE) {
    return;
  }

  throw new ApiError(
    503,
    "The request did not reach Field Notebook. Try again.",
  );
}

/**
 * Runs one mock call: waits, honours cancellation, applies the failure rate,
 * then returns a copy so a caller that mutates the result cannot corrupt the
 * fixtures. Replacing this body with fetch is the whole real-API change.
 */
export async function mockRequest<T>(
  options: RequestOptions | undefined,
  produce: () => T,
): Promise<T> {
  await waitForMockLatency(options?.signal);
  failTransiently();
  return structuredClone(produce());
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

/**
 * Local-development identity until Entra ID sign-in exists. The API reads the
 * user from this header and still checks project, role, and crew itself.
 */
const DEMO_USER_HEADER = "X-Demo-User-Id";

const UNREACHABLE = "The request did not reach Field Notebook. Try again.";

function requestHeaders(body: unknown): Record<string, string> {
  const headers: Record<string, string> = { Accept: "application/json" };
  const session = readAuthSession();

  // The browser supplies the multipart boundary for photo uploads.
  if (body !== undefined && !(body instanceof FormData)) {
    headers["Content-Type"] = "application/json";
  }

  if (session !== null) {
    headers[DEMO_USER_HEADER] = String(session.user.userId);
  }

  return headers;
}

/**
 * The error body every endpoint returns. `title` is the HTTP reason phrase,
 * such as "Not Found"; `detail` and `error.message` carry the same plain
 * sentence; `error.code` is the stable category, such as not_found.
 */
interface ApiProblem {
  title?: unknown;
  detail?: unknown;
  error?: { code?: unknown; message?: unknown } | null;
}

/** The body of a failed response, or null when it is not JSON. */
async function readProblem(response: Response): Promise<ApiProblem | null> {
  try {
    return (await response.json()) as ApiProblem;
  } catch {
    return null;
  }
}

function problemCode(problem: ApiProblem | null): string | null {
  return typeof problem?.error?.code === "string" ? problem.error.code : null;
}

function nonEmpty(value: unknown): string | null {
  return typeof value === "string" && value.trim() !== "" ? value.trim() : null;
}

/**
 * The sentence a page shows for a failed response: the structured message
 * first, then the detail, then the bare reason phrase as a last resort.
 */
function problemMessage(problem: ApiProblem | null): string {
  return nonEmpty(problem?.error?.message) ?? nonEmpty(problem?.detail) ?? nonEmpty(problem?.title) ?? UNREACHABLE;
}

function encodeBody(body: unknown): BodyInit | undefined {
  if (body === undefined || body instanceof FormData) return body
  return JSON.stringify(body)
}

/**
 * One request against the Field Notebook API. Cancellation surfaces as the
 * same AbortError the mock raises; any other failure becomes an ApiError whose
 * message a page can show as it is.
 */
async function apiRequest<T>(
  method: "GET" | "POST" | "PUT" | "PATCH",
  path: string,
  options?: RequestOptions,
  body?: unknown,
): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers: requestHeaders(body),
      body: encodeBody(body),
      signal: options?.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw error;
    }

    throw new ApiError(503, UNREACHABLE);
  }

  if (!response.ok) {
    const problem = await readProblem(response);

    throw new ApiError(response.status, problemMessage(problem), problemCode(problem));
  }

  return (await response.json()) as T;
}

export function apiGet<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiRequest<T>("GET", path, options);
}

/** A POST with no body; the route names the action. */
export function apiPost<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiRequest<T>("POST", path, options);
}

/** A PUT with no body; repeating it leaves the same state. */
export function apiPut<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiRequest<T>("PUT", path, options);
}

export function apiPostForm<T>(path: string, body: FormData, options?: RequestOptions): Promise<T> {
  return apiRequest<T>('POST', path, options, body)
}

/** A POST whose JSON body carries the new sub-resource. */
export function apiPostJson<T>(
  path: string,
  body: unknown,
  options?: RequestOptions,
): Promise<T> {
  return apiRequest<T>("POST", path, options, body);
}

/** A PATCH whose JSON body carries only the fields being changed. */
export function apiPatch<T>(
  path: string,
  body: unknown,
  options?: RequestOptions,
): Promise<T> {
  return apiRequest<T>("PATCH", path, options, body);
}
