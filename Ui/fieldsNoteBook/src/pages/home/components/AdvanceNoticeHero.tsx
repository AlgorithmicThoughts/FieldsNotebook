import { ArrowRight, CalendarDays, LockKeyhole } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'

export interface AdvanceNotice {
  /** Delays.Number of the advance delay. */
  delayNumber: string
  headline: string
  detail: string
  ownerCrewName: string
  blockedCrewNames: string[]
  /** Time part of DelayCycles.ExpectedReadyAt in the project's local time. */
  expectedReadyTimeLabel: string
  /** Day part of the same tentative completion time. */
  expectedReadyDayLabel: string
}

interface AdvanceNoticeHeroProps {
  notice: AdvanceNotice
  onOpenDelay?: (delayNumber: string) => void
}

/** Advance notice of a blocker that stops tomorrow's planned work. */
export function AdvanceNoticeHero({ notice, onOpenDelay }: AdvanceNoticeHeroProps) {
  return (
    <section className="hero">
      <div className="hero-content">
        <Badge label="Ahead of tomorrow" tone="blocked" icon={CalendarDays} />
        <h2>{notice.headline}</h2>
        <p>{notice.detail}</p>
        <div className="hero-actions">
          <Button
            label="View delay"
            variant="primary"
            icon={ArrowRight}
            onClick={() => onOpenDelay?.(notice.delayNumber)}
          />
          <span className="small muted">
            {notice.delayNumber} · {notice.ownerCrewName} → {notice.blockedCrewNames.join(', ')}
          </span>
        </div>
      </div>

      <div className="hero-estimate">
        <div className="eyebrow">Tentative completion</div>
        <div className="eta">{notice.expectedReadyTimeLabel}</div>
        <strong>{notice.expectedReadyDayLabel}</strong>
        <small>
          <Icon icon={LockKeyhole} size={13} /> An estimate is not clearance to start.
        </small>
      </div>
    </section>
  )
}
