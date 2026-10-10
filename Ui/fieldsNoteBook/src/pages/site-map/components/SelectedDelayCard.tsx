import { ArrowRight, Image, MapPin, MapPinOff, Pin } from 'lucide-react'
import type { DelayPhotoPreview } from '@/api/siteMaps'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { Icon } from '@/components/common/Icon'
import { DELAY_STATUS_DISPLAY, type DelaySummary } from '@/domain/delays'
import { formatProjectDate } from '@/lib/projectTime'
import { MapEmpty } from './MapEmpty'

/** Approved photo metrics beside the map: 155 px image and an 11 px caption. */
const PHOTO_FRAME = 'mt-[18px] overflow-hidden rounded-[var(--radius-control)] border border-border'

const PHOTO_PLACEHOLDER =
  'grid h-[155px] place-content-center justify-items-center gap-2 bg-[var(--bg)] text-[10px] tracking-[1px] text-muted-foreground uppercase'

interface DelayPhotoProps {
  photo: DelayPhotoPreview | null
}

function DelayPhoto({ photo }: DelayPhotoProps) {
  if (photo === null) {
    return (
      <div className={PHOTO_FRAME}>
        <div className={PHOTO_PLACEHOLDER}>
          <Icon icon={Image} size={34} />
          No photo
        </div>
      </div>
    )
  }

  return (
    <div className={PHOTO_FRAME}>
      <img
        src={photo.thumbnailUrl}
        alt={photo.fileName}
        className="block h-[155px] w-full bg-[var(--bg)] object-cover"
      />
      <div className="p-[10px] text-[11px] [overflow-wrap:anywhere]">
        <strong className="mb-1 block">{photo.fileName}</strong>
        <p className="mt-1 text-muted-foreground">
          Captured: {formatProjectDate(photo.createdAt)}
        </p>
      </div>
    </div>
  )
}

interface SelectedDelayCardProps {
  delay: DelaySummary | null
  photo: DelayPhotoPreview | null
  onOpenDelay: () => void
}

/** The selected pin's delay, beside the map. Read-only; no pin editing here. */
export function SelectedDelayCard({ delay, photo, onOpenDelay }: SelectedDelayCardProps) {
  if (delay === null) {
    return (
      <Card>
        <MapEmpty
          icon={MapPinOff}
          title="No active pins"
          detail="A delay reaches the map once a reporter places its pin."
        />
      </Card>
    )
  }

  const display = DELAY_STATUS_DISPLAY[delay.statusCode]

  return (
    <Card>
      <div className="p-[22px]">
        <div className="row wrap">
          <Badge label={display.label} tone={display.tone} icon={display.icon} />
          <Badge label={delay.number} tone="outline" />
        </div>

        <h2 className="mt-4">{delay.title}</h2>

        <div className="row wrap mt-4">
          <Badge label={delay.tagName} tone="outline" />
          {delay.isHighPriority ? <Badge label="High priority" tone="urgent" icon={Pin} /> : null}
        </div>

        <div className="location-line">
          <Icon icon={MapPin} />
          <div>
            <strong>{delay.locationName}</strong>
            <small>Manual site pin · the site image has no GPS calibration</small>
          </div>
        </div>

        <DelayPhoto photo={photo} />

        <p className="small muted mt-4">
          {delay.ownerCrewName} → {delay.blockedCrewNames.join(', ')}
        </p>

        <Button
          label="Open delay"
          variant="primary"
          icon={ArrowRight}
          className="mt-[18px] w-full"
          onClick={onOpenDelay}
        />
      </div>
    </Card>
  )
}
