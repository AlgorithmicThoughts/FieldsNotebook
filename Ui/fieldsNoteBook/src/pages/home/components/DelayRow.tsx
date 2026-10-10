import { ArrowRight, ChevronRight, Pin, TriangleAlert } from 'lucide-react'
import { DELAY_STATUS_DISPLAY, DELAY_TAG_ICON, type DelaySummary } from '@/domain/delays'
import { Badge } from '@/components/common/Badge'
import { Icon } from '@/components/common/Icon'

interface DelayRowProps {
  delay: DelaySummary
  onOpen?: (delayNumber: string) => void
}

export function DelayRow({ delay, onOpen }: DelayRowProps) {
  const status = DELAY_STATUS_DISPLAY[delay.statusCode]

  return (
    <button
      type="button"
      className="delay-row"
      onClick={() => onOpen?.(delay.number)}
      aria-label={`Open ${delay.number}: ${delay.title}`}
    >
      <span className={delay.isHighPriority ? 'delay-symbol urgent' : 'delay-symbol'}>
        <Icon icon={delay.isHighPriority ? Pin : DELAY_TAG_ICON[delay.tagCode]} />
      </span>

      <span className="grow">
        <span className="row wrap delay-meta">
          <span>{delay.number}</span>
          <span>·</span>
          <span>{delay.tagName}</span>
          {delay.isHighPriority ? (
            <Badge label="High priority" tone="urgent" icon={TriangleAlert} />
          ) : null}
        </span>

        <strong>{delay.title}</strong>

        <p>
          {delay.ownerCrewName} <Icon icon={ArrowRight} size={13} />{' '}
          {delay.blockedCrewNames.join(', ')} · {delay.locationName}
          <br />
          <span className="delay-project">{delay.projectName}</span>
        </p>
      </span>

      <span className="row-end">
        <Badge label={status.label} tone={status.tone} icon={status.icon} />
        <Icon icon={ChevronRight} />
      </span>
    </button>
  )
}
