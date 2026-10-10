import { CircleCheck, CirclePause } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import type { DelayCycleDetail } from '@/domain/delays'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'

function momentLabel(value: string): string {
  return `${formatProjectDayLabel(value)} · ${formatProjectTime(value)}`
}

/**
 * DelayCycles.AcknowledgmentChoice is an open string such as `CrewWillClear`.
 * Splitting it for display invents no label and no contract.
 */
function readableChoice(choice: string): string {
  const words = choice.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase()

  return words.charAt(0).toUpperCase() + words.slice(1)
}

interface DelayCycleListProps {
  /** Oldest first. The last entry is the cycle the delay is living in. */
  cycles: DelayCycleDetail[]
}

/** Every cycle of one delay. A reopen adds a cycle and overwrites nothing. */
export function DelayCycleList({ cycles }: DelayCycleListProps) {
  const currentCycleId = cycles.at(-1)?.id

  return (
    <section className="detail-section">
      <h3>Cycle history</h3>
      <p>
        Cycle one is the report. Each later cycle came from a reopen and carries its own
        acknowledgment and its own handover checks.
      </p>

      {cycles.map((cycle) => (
        <div
          key={cycle.id}
          className={cycle.id === currentCycleId ? 'detail-panel current' : 'detail-panel'}
        >
          <div className="row wrap">
            <strong>Cycle {cycle.sequence}</strong>
            {cycle.closedAt === null ? (
              <Badge label="Open" tone="progress" icon={CirclePause} />
            ) : (
              <Badge label="Closed" tone="resolved" icon={CircleCheck} />
            )}
          </div>

          <p>Opened {momentLabel(cycle.createdAt)}</p>

          {cycle.openReason === null ? null : <p>Reopened because {cycle.openReason}</p>}

          {cycle.acknowledgedAt === null ? (
            <p>Not acknowledged yet.</p>
          ) : (
            <p>
              Acknowledged by {cycle.acknowledgedByName} · {momentLabel(cycle.acknowledgedAt)}
              {cycle.acknowledgmentChoice === null
                ? ''
                : ` · ${readableChoice(cycle.acknowledgmentChoice)}`}
            </p>
          )}

          {cycle.acknowledgmentNote === null ? null : <p>“{cycle.acknowledgmentNote}”</p>}

          <p>
            {cycle.expectedReadyAt === null
              ? 'No tentative completion time for this cycle.'
              : `Tentative completion ${momentLabel(cycle.expectedReadyAt)}. The time alone does not clear the blocker.`}
          </p>

          {cycle.closedAt === null ? null : <p>Closed {momentLabel(cycle.closedAt)}</p>}
        </div>
      ))}
    </section>
  )
}
