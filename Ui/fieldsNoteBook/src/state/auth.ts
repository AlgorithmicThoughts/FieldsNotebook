import { createContext, useContext } from 'react'
import type { Credentials } from '@/api/auth'
import type { AuthSession } from '@/domain/auth'

export interface AuthState {
  /** The signed-in session, or null while nobody is signed in. */
  session: AuthSession | null
  /** Rejects with the ApiError the sign-in form renders. */
  signIn: (credentials: Credentials) => Promise<void>
  signOut: () => void
}

export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const state = useContext(AuthContext)

  if (state === null) {
    throw new Error('useAuth needs an AuthProvider above it.')
  }

  return state
}
