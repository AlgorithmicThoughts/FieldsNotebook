import type { AuthSession } from '@/domain/auth'

/**
 * The browser-local store for the signed-in session. A cookie keeps it readable
 * by a future API call; replacing this module is the whole change when real
 * tokens arrive.
 */
const COOKIE_NAME = 'fieldnotebook.auth'
const MAX_AGE_SECONDS = 60 * 60 * 24 * 30

function readCookie(name: string): string | null {
  const entry = document.cookie
    .split('; ')
    .find((candidate) => candidate.startsWith(`${name}=`))

  return entry ? entry.slice(name.length + 1) : null
}

/** A cookie can hold anything, so the shape is checked before it is trusted. */
function toSession(value: unknown): AuthSession | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }

  const record = value as Record<string, unknown>
  const user = record.user as Record<string, unknown> | undefined

  if (record.authenticated !== true || typeof user?.userId !== 'number') {
    return null
  }

  return { authenticated: true, user: { userId: user.userId } }
}

/** The stored session, or null when nothing valid is stored. */
export function readAuthSession(): AuthSession | null {
  const raw = readCookie(COOKIE_NAME)

  if (raw === null) {
    return null
  }

  try {
    return toSession(JSON.parse(decodeURIComponent(raw)))
  } catch {
    return null
  }
}

export function writeAuthSession(session: AuthSession): void {
  const value = encodeURIComponent(JSON.stringify(session))

  document.cookie = `${COOKIE_NAME}=${value}; path=/; max-age=${MAX_AGE_SECONDS}; samesite=lax`
}

export function clearAuthSession(): void {
  document.cookie = `${COOKIE_NAME}=; path=/; max-age=0; samesite=lax`
}
