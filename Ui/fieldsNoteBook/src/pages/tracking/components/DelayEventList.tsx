import { describeActivityEvent } from '@/domain/activity'
import type { DelayEventDetail } from '@/domain/delays'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'

/** A completion-time change appends an event; it never creates a cycle. */
function estimateLine(event: DelayEventDetail): string | null {
  if (event.newExpectedReadyAt === null) {
    return null
  }

  const next = `${formatProjectDayLabel(event.newExpectedReadyAt)} · ${formatProjectTime(event.newExpectedReadyAt)}`

  if (event.oldExpectedReadyAt === null) {
    return `Tentative completion ${next}`
  }

  return `Tentative completion moved from ${formatProjectTime(event.oldExpectedReadyAt)} to ${next}`
}

interface DelayEventListProps {
  /** Oldest first. Every action stays in the history. */
  events: DelayEventDetail[]
}

export function DelayEventList({ events }: DelayEventListProps) {
  return (
    <section className="detail-section">
      <h3>Activity</h3>

      {events.length === 0 ? <p>No activity is recorded for this delay.</p> : null}

      <div className="detail-events">
        {events.map((event) => {
          const estimate = estimateLine(event)

          return (
            <div key={event.id} className="detail-event">
              <strong>{describeActivityEvent(event.eventType)}</strong>
              <small>
                {event.actorName} · {formatProjectDayLabel(event.createdAt)} ·{' '}
                {formatProjectTime(event.createdAt)}
                {event.cycleSequence === null ? '' : ` · Cycle ${event.cycleSequence}`}
              </small>
              {estimate === null ? null : <small>{estimate}</small>}
              {event.returnReason === null ? null : <small>Sent back: {event.returnReason}</small>}
            </div>
          )
        })}
      </div>
    </section>
  )
}
