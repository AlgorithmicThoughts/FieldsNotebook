/**
 * A failure the API reported. `message` is plain language a page can render as
 * it stands; pages do not write their own copy for a failed request.
 */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

/** A cancelled request is not a failure. Callers must tell the two apart. */
export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}
