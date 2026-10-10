export interface ActivityEntry {
  /** ActivityEvents.Id. */
  id: string
  title: string
  detail: string
  /** ActivityEvents.CreatedAt in the project's local time. */
  timeLabel: string
  /** Delays.Number that the event belongs to. */
  delayNumber: string
}

interface ActivityTimelineProps {
  entries: ActivityEntry[]
}

export function ActivityTimeline({ entries }: ActivityTimelineProps) {
  return (
    <div className="timeline">
      {entries.map((entry) => (
        <div key={entry.id} className="timeline-item">
          <strong>{entry.title}</strong>
          <p>{entry.detail}</p>
          <small>
            {entry.timeLabel} · {entry.delayNumber}
          </small>
        </div>
      ))}
    </div>
  )
}
