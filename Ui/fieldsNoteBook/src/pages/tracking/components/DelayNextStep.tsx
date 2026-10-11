import { useState } from 'react'
import { Button } from '@/components/common/Button'
import type { DelayDetail } from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import {
  EMPTY_ACTION_DRAFT,
  submitsImmediately,
  verifiableImpacts,
  type DelayActionDraft,
} from '../delayActionDraft'
import { useDelayAction } from '../useDelayAction'
import type { DelayActionId, DelayActionOffer } from '../trackingView'
import { DelayActionForm } from './DelayActionForm'

interface DelayNextStepProps {
  /** What this user does next, in one sentence. */
  note: string
  offers: DelayActionOffer[]
  delay: DelayDetail
  user: CurrentUser
  /** Reloads the delay and the tracking list once the API accepts an action. */
  onCompleted: () => void
}

/** The action card under the handover progress: the next step, then its controls. */
export function DelayNextStep({ note, offers, delay, user, onCompleted }: DelayNextStepProps) {
  const [openAction, setOpenAction] = useState<DelayActionId | null>(null)
  const [draft, setDraft] = useState<DelayActionDraft>(EMPTY_ACTION_DRAFT)
  // The accepted action closes its own form; the reloaded delay offers what comes next.
  const { pending, error, submit, clearError } = useDelayAction(delay, () => {
    setOpenAction(null)
    setDraft(EMPTY_ACTION_DRAFT)
    onCompleted()
  })

  function open(action: DelayActionId) {
    clearError()

    if (submitsImmediately(action)) {
      submit(action, draft)
      return
    }

    // A Foreman who covers one affected crew never has to choose it.
    const [onlyImpact] = verifiableImpacts(delay, user)

    setDraft({ ...EMPTY_ACTION_DRAFT, impactId: onlyImpact?.id ?? null })
    setOpenAction(action)
  }

  function close() {
    clearError()
    setOpenAction(null)
  }

  return (
    <section className="action-panel">
      <div className="eyebrow">Your next step</div>
      <p>{note}</p>

      {offers.map((offer) => (
        <Button
          key={offer.id}
          label={offer.label}
          icon={offer.icon}
          variant={offer.id === openAction ? 'primary' : 'default'}
          disabled={pending !== null}
          onClick={() => open(offer.id)}
        />
      ))}

      {openAction === null ? null : (
        <DelayActionForm
          action={openAction}
          delay={delay}
          user={user}
          draft={draft}
          pending={pending === openAction}
          onDraftChange={(patch) => setDraft((current) => ({ ...current, ...patch }))}
          onSubmit={() => submit(openAction, draft)}
          onCancel={close}
        />
      )}

      {error === null ? null : (
        <p role="alert" className="action-error">
          {error}
        </p>
      )}
    </section>
  )
}
