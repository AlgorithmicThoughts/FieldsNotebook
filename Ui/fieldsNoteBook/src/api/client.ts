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
