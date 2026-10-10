import type { SiteMapPin } from '@/components/delays/SiteMap'
import { DELAY_STATUS_DISPLAY, type DelayStatusCode, type DelaySummary } from '@/domain/delays'

const STATUS_ORDER = Object.keys(DELAY_STATUS_DISPLAY) as DelayStatusCode[]

/** Only delays that carry a pin reach the image. */
export function toSiteMapPins(delays: DelaySummary[]): SiteMapPin[] {
  return delays.flatMap((delay) =>
    delay.pin === null
      ? []
      : [
          {
            delayNumber: delay.number,
            title: delay.title,
            statusCode: delay.statusCode,
            pin: delay.pin,
          },
        ],
  )
}

/** The statuses the map actually shows, in the approved status order. */
export function toLegendStatuses(pins: SiteMapPin[]): DelayStatusCode[] {
  return STATUS_ORDER.filter((code) => pins.some((item) => item.statusCode === code))
}
