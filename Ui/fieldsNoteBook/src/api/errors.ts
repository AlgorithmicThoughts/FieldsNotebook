/**
 * A failure the API reported. `message` is plain language a page can render as
 * it stands; pages do not write their own copy for a failed request.
 */
export class ApiError extends Error {
  readonly status: number
  /** The API's stable category, such as not_found or conflict, when it sent one. */
  readonly code: string | null

  constructor(status: number, message: string, code: string | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

/** A cancelled request is not a failure. Callers must tell the two apart. */
export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

/**
 * The text a page shows for a failed request: the API's own message, or the
 * page's fallback when the failure never came from the API.
 */
export function errorMessage(error: unknown, fallback: string): string {
  return isApiError(error) ? error.message : fallback
}
