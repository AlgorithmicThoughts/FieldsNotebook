const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''
const DEMO_USER_ID = import.meta.env.VITE_DEMO_USER_ID
const DEMO_USER_HEADER = 'X-Demo-User-Id'

/** A non-success HTTP response, with the ProblemDetails title when the API supplies one. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export async function apiGet<T>(path: string, signal: AbortSignal): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (DEMO_USER_ID) {
    headers[DEMO_USER_HEADER] = DEMO_USER_ID
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { headers, signal })
  if (!response.ok) {
    throw new ApiError(response.status, await readProblemTitle(response))
  }
  return (await response.json()) as T
}

async function readProblemTitle(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { title?: unknown }
    return typeof problem.title === 'string' ? problem.title : response.statusText
  } catch {
    return response.statusText
  }
}
