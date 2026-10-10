import { Button } from '@/components/common/Button'
import type { DelayActionOffer } from '../trackingView'

/**
 * Acknowledge, request a handover check, verify, resolve, reopen, and revoke
 * all need write endpoints that do not exist yet, so each control the user is
 * entitled to appears disabled. A state change faked in local state would
 * vanish on the next request, which is worse than a disabled control.
 */
const PENDING_NOTE = 'These actions arrive with the API.'

interface DelayNextStepProps {
  /** What this user does next, in one sentence. */
  note: string
  offers: DelayActionOffer[]
}

/** The action card under the handover progress: the next step, then its controls. */
export function DelayNextStep({ note, offers }: DelayNextStepProps) {
  return (
    <section className="action-panel">
      <div className="eyebrow">Your next step</div>
      <p>{note}</p>

      {offers.map((offer) => (
        <Button key={offer.id} label={offer.label} icon={offer.icon} disabled />
      ))}

      {offers.length === 0 ? null : <p>{PENDING_NOTE}</p>}
    </section>
  )
}
