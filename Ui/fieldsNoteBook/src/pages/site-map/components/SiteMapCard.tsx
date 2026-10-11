import { MapPin, MapPinOff } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Card, CardHeading } from '@/components/common/Card'
import { SiteMap, type SiteMapPin } from '@/components/delays/SiteMap'
import { DELAY_STATUS_DISPLAY, type SiteMapOption } from '@/domain/delays'
import { toLegendStatuses } from '../siteMapView'
import { MapEmpty } from './MapEmpty'

const MAP_NOTE =
  'Illustrative site image. A pin is the point the reporter chose; photo GPS never becomes a pin, because this image has no calibration.'

interface SiteMapCardProps {
  siteMap: SiteMapOption | null
  pins: SiteMapPin[]
  selectedDelayNumber: string | null
  onSelectPin: (delayNumber: string) => void
}

export function SiteMapCard({
  siteMap,
  pins,
  selectedDelayNumber,
  onSelectPin,
}: SiteMapCardProps) {
  return (
    <Card className="overflow-hidden">
      <CardHeading
        title={siteMap?.name ?? 'Site map'}
        action={
          <Badge
            label={`${pins.length} ${pins.length === 1 ? 'pin' : 'pins'}`}
            tone="outline"
            icon={MapPin}
          />
        }
      />
      <SiteMap
        imageName={siteMap?.name ?? 'Site map'}
        pins={pins}
        selectedDelayNumber={selectedDelayNumber ?? undefined}
        onSelectPin={onSelectPin}
      />
      {pins.length === 0 ? (
        <MapEmpty
          icon={MapPinOff}
          title="No delay carries a pin"
          detail="The map is ready. A delay reaches it once a reporter places its pin."
        />
      ) : (
        <div className="map-legend">
          {toLegendStatuses(pins).map((code) => {
            const display = DELAY_STATUS_DISPLAY[code]

            return (
              <Badge key={code} label={display.label} tone={display.tone} icon={display.icon} />
            )
          })}
        </div>
      )}
      <p className="map-note">{MAP_NOTE}</p>
    </Card>
  )
}
