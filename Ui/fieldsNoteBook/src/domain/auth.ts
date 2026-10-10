/** The stored proof of sign-in. Only a signed-in session is ever written. */
export interface AuthSession {
  authenticated: true
  user: { userId: number }
}
