import { Building2, CalendarDays, MapPin, Pin } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Icon } from '@/components/common/Icon'
import { Modal } from '@/components/common/Modal'
import { SiteMap } from '@/components/delays/SiteMap'
import {
  DELAY_STATUS_DISPLAY,
  DELAY_TAG_ICON,
  type DelaySummary,
} from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'
import { toHandoverSteps, toNextStepNote, toOfferedActions } from '../trackingView'
import { useDelayDetail, type DelayDetailData } from '../useDelayDetail'
import { DelayCycleList } from './DelayCycleList'
import { DelayEventList } from './DelayEventList'
import { DelayHandoverProgress } from './DelayHandoverProgress'
import { DelayImpactList } from './DelayImpactList'
import { DelayNextStep } from './DelayNextStep'
import { DelayPhotoGrid } from './DelayPhotoGrid'

interface DelayDetailBodyProps extends DelayDetailData {
  user: CurrentUser
}

function DelayDetailBody({ delay, siteMap, user }: DelayDetailBodyProps) {
  const status = DELAY_STATUS_DISPLAY[delay.statusCode]
  const currentCycle = delay.cycles.at(-1)
  const pin = delay.pin

  return (
    <div className="detail-stack">
      <div className="row wrap">
        <Badge label={status.label} tone={status.tone} icon={status.icon} />
        <Badge label={delay.tagName} tone="outline" icon={DELAY_TAG_ICON[delay.tagCode]} />
        {delay.isHighPriority ? <Badge label="High priority" tone="urgent" icon={Pin} /> : null}
        {delay.reportType === 'AdvanceNotice' ? (
          <Badge label="Advance notice" tone="progress" icon={CalendarDays} />
        ) : null}
        <Badge label={delay.projectName} tone="outline" icon={Building2} />
      </div>

      <div className="detail-grid">
        <div className="detail-stack">
          <section className="detail-section">
            <h2>{delay.title}</h2>
            {delay.description === null ? null : <p>{delay.description}</p>}
            <p>
              Raised by {delay.reportedBy.displayName} · {delay.reportedBy.crewName} ·{' '}
              {formatProjectDayLabel(delay.reportedBy.reportedAt)} ·{' '}
              {formatProjectTime(delay.reportedBy.reportedAt)}
            </p>
            <p>{delay.ownerCrewName} must remove the blocker.</p>
          </section>

          <div className="location-line">
            <Icon icon={MapPin} />
            <div>
              <strong>{delay.locationName}</strong>
              <small>
                {delay.segmentName ?? 'No segment recorded'} · the site image has no GPS
                calibration, so only a chosen pin is a position
              </small>
            </div>
          </div>

          {siteMap !== null && pin !== null && delay.siteMapId === siteMap.id ? (
            <SiteMap
              key={siteMap.uri}
              imageName={siteMap.name}
              imageUri={siteMap.uri}
              pins={[
                {
                  delayNumber: delay.number,
                  title: delay.title,
                  statusCode: delay.statusCode,
                  pin,
                },
              ]}
              selectedDelayNumber={delay.number}
              compact
            />
          ) : null}

          <DelayImpactList
            impacts={delay.impacts}
            currentCycleSequence={currentCycle?.sequence ?? 1}
          />
          <DelayCycleList cycles={delay.cycles} />
          <DelayPhotoGrid groups={delay.photos} />
          <DelayEventList events={delay.events} />
        </div>

        <aside className="detail-aside">
          <DelayHandoverProgress steps={toHandoverSteps(delay)} />
          <DelayNextStep
            note={toNextStepNote(delay, user)}
            offers={toOfferedActions(delay, user)}
          />
        </aside>
      </div>
    </div>
  )
}

interface DelayDetailModalProps {
  /** The row the user opened, or null when nothing is open. */
  summary: DelaySummary | null
  user: CurrentUser
  onClose: () => void
}

/** One delay in full: its author, cycles, affected crews, photos, and history. */
export function DelayDetailModal({ summary, user, onClose }: DelayDetailModalProps) {
  const state = useDelayDetail(summary?.id ?? null, summary?.projectId ?? null)

  if (summary === null) {
    return null
  }

  const data = state.key !== null && state.status === 'ready' ? state.data : null
  const errorMessage = state.key !== null && state.status === 'error' ? state.message : null

  return (
    <Modal open wide title={summary.number} onOpenChange={onClose}>
      {data === null && errorMessage === null ? (
        <p className="py-6 text-center text-[13px] text-muted-foreground">
          Loading the cycles, crews, and photos for {summary.number}…
        </p>
      ) : null}

      {errorMessage === null ? null : (
        <p
          role="alert"
          className="rounded-[var(--radius-control)] bg-[var(--urgent-bg)] px-[14px] py-3 text-[13px] text-[var(--urgent)]"
        >
          {errorMessage}
        </p>
      )}

      {data === null ? null : <DelayDetailBody {...data} user={user} />}
    </Modal>
  )
}
