import { CircleCheck, ClipboardCheck } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import type { DelayImpactDetail, DelayVerification } from '@/domain/delays'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'

function momentLabel(value: string): string {
  return `${formatProjectDayLabel(value)} · ${formatProjectTime(value)}`
}

function VerificationLine({ verification }: { verification: DelayVerification }) {
  return (
    <p className="detail-verified">
      Verified by {verification.verifiedByName} · {momentLabel(verification.verifiedAt)}
      {verification.note === null ? '' : ` · “${verification.note}”`}
    </p>
  )
}

interface DelayImpactListProps {
  /** Every DelayImpacts row. The list is never reduced to one crew. */
  impacts: DelayImpactDetail[]
  /** The cycle the shown checks belong to. */
  currentCycleSequence: number
}

/**
 * Each affected crew, the work item it cannot start, and its acceptances
 * for the current cycle. A reopened cycle starts with none of them.
 */
export function DelayImpactList({ impacts, currentCycleSequence }: DelayImpactListProps) {
  return (
    <section className="detail-section">
      <h3>Affected crews</h3>
      <p>
        Each affected crew accepts the proof of work for itself and is unblocked once it does.
        Acceptances below belong to cycle {currentCycleSequence} only.
      </p>

      {impacts.map((impact) => (
        <div key={impact.id} className="detail-panel">
          <div className="row wrap">
            <strong>{impact.crewName}</strong>
            {impact.verifications.length === 0 ? (
              <Badge label="Still blocked" tone="verify" icon={ClipboardCheck} />
            ) : (
              <Badge label="Accepted · unblocked" tone="resolved" icon={CircleCheck} />
            )}
          </div>

          <p>
            {impact.workItemTitle === null
              ? 'No blocked work item recorded.'
              : `Cannot start: ${impact.workItemTitle}`}
          </p>

          {impact.verifications.map((verification) => (
            <VerificationLine key={verification.verifiedAt} verification={verification} />
          ))}
        </div>
      ))}

      {impacts.length === 0 ? <p>No affected crew is recorded for this delay.</p> : null}
    </section>
  )
}
