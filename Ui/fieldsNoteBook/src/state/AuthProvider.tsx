import { useState, type ReactNode } from 'react'
import { signIn as requestSignIn, type Credentials } from '@/api/auth'
import type { AuthSession } from '@/domain/auth'
import { AuthContext, type AuthState } from './auth'
import { clearAuthSession, readAuthSession, writeAuthSession } from './authCookie'

interface AuthProviderProps {
  children: ReactNode
}

/** Owns the signed-in session and keeps the stored copy in step with it. */
export function AuthProvider({ children }: AuthProviderProps) {
  const [session, setSession] = useState<AuthSession | null>(readAuthSession)

  async function signIn(credentials: Credentials) {
    const next = await requestSignIn(credentials)

    writeAuthSession(next)
    setSession(next)
  }

  function signOut() {
    clearAuthSession()
    setSession(null)
  }

  const state: AuthState = { session, signIn, signOut }

  return <AuthContext value={state}>{children}</AuthContext>
}
