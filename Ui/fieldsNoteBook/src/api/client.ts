import { readAuthSession } from '@/state/authCookie'
import { ApiError } from './errors'
import type { RequestOptions } from './types'

/**
 * Artificial latency ceiling in milliseconds. Each call waits a fraction of it
 * so loading states are visible during development. Set to 0 to remove the
 * wait entirely.
 */
export const MOCK_LATENCY_MS = 400

/** Share of mock calls that fail with a transient error. Set to 0 to disable. */
export const MOCK_FAILURE_RATE = 0

function abortError(): DOMException {
  return new DOMException('Aborted', 'AbortError')
}

function randomLatencyMs(): number {
  return Math.round(MOCK_LATENCY_MS * (0.4 + Math.random() * 0.6))
}

/** Resolves after the mock latency, or rejects if the caller aborts first. */
function waitForMockLatency(signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(abortError())
      return
    }

    const onAbort = () => {
      clearTimeout(timer)
      reject(abortError())
    }

    const timer = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort)
      resolve()
    }, randomLatencyMs())

    signal?.addEventListener('abort', onAbort, { once: true })
  })
}

function failTransiently(): void {
  if (Math.random() >= MOCK_FAILURE_RATE) {
    return
  }

  throw new ApiError(503, 'The request did not reach Field Notebook. Try again.')
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
  await waitForMockLatency(options?.signal)
  failTransiently()
  return structuredClone(produce())
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

/**
 * Local-development identity until Entra ID sign-in exists. The API reads the
 * user from this header and still checks project, role, and crew itself.
 */
const DEMO_USER_HEADER = 'X-Demo-User-Id'

const UNREACHABLE = 'The request did not reach Field Notebook. Try again.'

function requestHeaders(hasBody: boolean): Record<string, string> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  const session = readAuthSession()

  if (hasBody) {
    headers['Content-Type'] = 'application/json'
  }

  if (session !== null) {
    headers[DEMO_USER_HEADER] = String(session.user.userId)
  }

  return headers
}

async function problemTitle(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { title?: unknown }

    return typeof problem.title === 'string' ? problem.title : UNREACHABLE
  } catch {
    return UNREACHABLE
  }
}

/**
 * One request against the Field Notebook API. Cancellation surfaces as the
 * same AbortError the mock raises; any other failure becomes an ApiError whose
 * message a page can show as it is.
 */
async function apiRequest<T>(
  method: 'GET' | 'POST' | 'PATCH',
  path: string,
  options?: RequestOptions,
  body?: unknown,
): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers: requestHeaders(body !== undefined),
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: options?.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }

    throw new ApiError(503, UNREACHABLE)
  }

  if (!response.ok) {
    throw new ApiError(response.status, await problemTitle(response))
  }

  return (await response.json()) as T
}

export function apiGet<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiRequest<T>('GET', path, options)
}

/** A POST with no body; the route names the action. */
export function apiPost<T>(path: string, options?: RequestOptions): Promise<T> {
  return apiRequest<T>('POST', path, options)
}

/** A PATCH whose JSON body carries only the fields being changed. */
export function apiPatch<T>(path: string, body: unknown, options?: RequestOptions): Promise<T> {
  return apiRequest<T>('PATCH', path, options, body)
}
