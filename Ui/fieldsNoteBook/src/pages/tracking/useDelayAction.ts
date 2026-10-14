import { useState } from 'react'
import { errorMessage, isAbortError } from '@/api/errors'
import type { DelayDetail } from '@/domain/delays'
import {
  submitDelayAction,
  validateActionDraft,
  type DelayActionDraft,
} from './delayActionDraft'
import type { DelayActionId } from './trackingView'

const GENERIC_ERROR = 'Field Notebook could not record that action.'

export interface DelayActionSubmission {
  /** The action currently saving, or null. One save runs at a time. */
  pending: DelayActionId | null
  /** What the form still needs, or why the API refused. */
  error: string | null
  submit: (action: DelayActionId, draft: DelayActionDraft) => void
  clearError: () => void
}

/**
 * Sends one workflow action and reports the outcome. A refusal keeps the
 * user's input so they can correct it, and `onCompleted` reloads the delay
 * rather than patching a local copy the API never confirmed.
 */
export function useDelayAction(delay: DelayDetail, onCompleted: () => void): DelayActionSubmission {
  const [pending, setPending] = useState<DelayActionId | null>(null)
  const [error, setError] = useState<string | null>(null)

  function submit(action: DelayActionId, draft: DelayActionDraft) {
    if (pending !== null) {
      return
    }

    const incomplete = validateActionDraft(action, draft)

    if (incomplete !== null) {
      setError(incomplete)
      return
    }

    setPending(action)
    setError(null)

    submitDelayAction(action, delay, draft)
      .then(() => {
        setPending(null)
        onCompleted()
      })
      .catch((failure: unknown) => {
        setPending(null)

        if (isAbortError(failure)) {
          return
        }

        setError(errorMessage(failure, GENERIC_ERROR))
      })
  }

  return { pending, error, submit, clearError: () => setError(null) }
}
