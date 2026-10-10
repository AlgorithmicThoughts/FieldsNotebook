import type { AuthSession } from '@/domain/auth'
import { mockRequest } from './client'
import { ApiError } from './errors'
import { AUTH_CREDENTIALS } from './fixtures'
import type { RequestOptions } from './types'

/**
 * One message for an unknown user ID and for a wrong password, so the screen
 * never reveals which user IDs exist.
 */
const REJECTED = 'That user ID and password do not match a Field Notebook account.'

export interface Credentials {
  /** Users.Id as typed on the sign-in screen. */
  userId: string
  password: string
}

function resolveUserId({ userId, password }: Credentials): number {
  const entered = userId.trim()
  const credential = AUTH_CREDENTIALS.find(
    (row) => String(row.UserId) === entered && row.Password === password,
  )

  if (credential === undefined) {
    throw new ApiError(401, REJECTED)
  }

  return credential.UserId
}

/** Checks the credentials and returns the session to store. */
export async function signIn(
  credentials: Credentials,
  options?: RequestOptions,
): Promise<AuthSession> {
  return mockRequest(options, () => ({
    authenticated: true as const,
    user: { userId: resolveUserId(credentials) },
  }))
}
