import { useState, type FormEvent } from 'react'
import { ArrowRight, CalendarDays, CirclePause, FileText } from 'lucide-react'
import { isApiError } from '@/api/errors'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { FormField } from '@/components/common/Field'
import { Icon } from '@/components/common/Icon'
import { TextInput } from '@/components/common/Input'
import { Brand } from '@/components/layout/Brand'
import { useResolvedTheme } from '@/hooks/useResolvedTheme'
import { useAuth } from '@/state/auth'
import './SignInPage.css'

const SIGN_IN_FAILED = 'Field Notebook could not sign you in. Try again.'

const POINTS = [
  { icon: CirclePause, label: 'Track the delays affecting your crew' },
  { icon: FileText, label: 'Find project documents in one place' },
  { icon: CalendarDays, label: "Plan tomorrow with today's blockers" },
]

/** The first screen, shown until a session is stored. */
export function SignInPage() {
  // No user is known yet, so the browser preference is the only one available.
  useResolvedTheme('system')

  const { signIn } = useAuth()
  const [userId, setUserId] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSigningIn, setIsSigningIn] = useState(false)

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSigningIn) {
      return
    }

    setIsSigningIn(true)
    setError(null)

    signIn({ userId, password }).catch((failure: unknown) => {
      setIsSigningIn(false)
      setError(isApiError(failure) ? failure.message : SIGN_IN_FAILED)
    })
  }

  return (
    <div className="signin-screen">
      <section className="signin-hero">
        <Brand />

        <h1>
          A clear handover.
          <br />
          A confirmed start.
        </h1>

        <p>
          Keep your crew in sync, follow every handoff, and see what&apos;s ahead across your
          assigned projects.
        </p>

        <ul className="signin-points">
          {POINTS.map((point) => (
            <li key={point.label} className="signin-point">
              <Icon icon={point.icon} />
              {point.label}
            </li>
          ))}
        </ul>
      </section>

      <Card className="signin-card">
        <h2>Welcome back</h2>
        <p className="signin-intro">Sign in to your field workspace.</p>

        <form className="signin-form" onSubmit={onSubmit} noValidate>
          <FormField name="user-id" label="User ID">
            <TextInput
              value={userId}
              autoComplete="username"
              placeholder="Enter your user ID"
              onChange={(event) => setUserId(event.target.value)}
            />
          </FormField>

          <FormField name="password" label="Password">
            <TextInput
              type="password"
              value={password}
              autoComplete="current-password"
              placeholder="Enter your password"
              onChange={(event) => setPassword(event.target.value)}
            />
          </FormField>

          {error ? (
            <p className="signin-error" role="alert">
              {error}
            </p>
          ) : null}

          <Button
            type="submit"
            variant="primary"
            label={isSigningIn ? 'Signing in…' : 'Sign in'}
            icon={ArrowRight}
            iconPosition="end"
            disabled={isSigningIn}
            className="justify-between"
          />
        </form>

        <p className="signin-demo">Demo access · User ID 1 · Password admin</p>
      </Card>
    </div>
  )
}
