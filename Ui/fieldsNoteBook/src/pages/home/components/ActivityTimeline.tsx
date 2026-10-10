import type { ActivityEntry } from '@/domain/activity'

export type { ActivityEntry }

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
