import { Navigate } from 'react-router-dom'
import { useAuth } from '@/state/auth'
import { SignInPage } from './SignInPage'

/** Keeps a signed-in user out of the sign-in screen. */
export function SignInRoute() {
  const { session } = useAuth()

  if (session !== null) {
    return <Navigate to="/" replace />
  }

  return <SignInPage />
}
