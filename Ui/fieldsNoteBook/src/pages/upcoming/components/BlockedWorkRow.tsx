import { CalendarClock, CirclePause, Link2, TriangleAlert } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Icon } from '@/components/common/Icon'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'
import type { BlockedWorkRow as BlockedWork, CompletionEstimate } from '@/domain/upcoming'

function at(value: string): string {
  return `${formatProjectTime(value)}, ${formatProjectDayLabel(value)}`
}

/**
 * Every wording keeps the time tentative and names the field it came from.
 * A time that has passed still leaves the crew blocked.
 */
function completionSentence(ownerCrewName: string, completion: CompletionEstimate): string {
  if (completion.source === 'delayCycle') {
    return `${ownerCrewName} expects to be ready at ${at(completion.at)}. That is the tentative completion time on the delay's active cycle, not a promise that the work is ready.`
  }

  if (completion.source === 'workItem') {
    return `${ownerCrewName} has not given a completion time. The plan expects ${completion.workItemTitle} to finish at ${at(completion.at)}, which is the work item's expected finish and not an acknowledgment.`
  }

  return `${ownerCrewName} has not given a tentative completion time yet.`
}

interface BlockedWorkRowProps {
  row: BlockedWork
}

export function BlockedWorkRow({ row }: BlockedWorkRowProps) {
  return (
    <div className="upcoming-row">
      <div className="upcoming-when">
        <strong>{formatProjectTime(row.plannedStartAt)}</strong>
        <span className="small muted">Planned start</span>
        <span className="small muted">{formatProjectDayLabel(row.plannedStartAt)}</span>
      </div>

      <div className="grow">
        <div className="row wrap upcoming-head">
          <h3>
            {row.blockedCrewName} · {row.workItemTitle}
          </h3>
          <Badge label="Cannot start" tone="blocked" icon={CirclePause} />
          {row.isAdvanceNotice ? (
            <Badge label="Advance notice" tone="outline" icon={CalendarClock} />
          ) : null}
          {row.isHighPriority ? (
            <Badge label="High priority" tone="urgent" icon={TriangleAlert} />
          ) : null}
        </div>

        <p className="upcoming-meta">
          {row.delayNumber} · {row.tagName}
          {row.segmentName ? ` · ${row.segmentName}` : ''} · {row.delayTitle}
        </p>

        <p className="upcoming-sentence">{completionSentence(row.ownerCrewName, row.completion)}</p>

        {row.blockedPredecessor ? (
          <p className="upcoming-predecessor">
            <Icon icon={Link2} size={16} />
            {row.workItemTitle} waits on {row.blockedPredecessor.title}, which{' '}
            {row.blockedPredecessor.delayNumber} still blocks.
          </p>
        ) : null}
      </div>
    </div>
  )
}
