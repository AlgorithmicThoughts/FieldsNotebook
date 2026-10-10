import { useState, type KeyboardEvent, type MouseEvent } from 'react'
import { MapPin } from 'lucide-react'
import type { DelayPin, SiteMapOption } from '@/domain/delays'
import { Icon } from '@/components/common/Icon'

/** One arrow press moves the pin two percent of the image. */
const PIN_STEP = 0.02

const KEY_STEPS: Record<string, DelayPin> = {
  ArrowUp: { x: 0, y: -PIN_STEP },
  ArrowDown: { x: 0, y: PIN_STEP },
  ArrowLeft: { x: -PIN_STEP, y: 0 },
  ArrowRight: { x: PIN_STEP, y: 0 },
}

/** Pin coordinates run from zero to one inside the image. */
function clamp(value: number): number {
  return Math.min(1, Math.max(0, value))
}

interface SiteMapPinPickerProps {
  siteMap: SiteMapOption | null
  pin: DelayPin | null
  onPinChange: (pin: DelayPin) => void
}

/**
 * A user-placed pin on the project site image. Nothing here derives a position
 * from photo GPS: the image has no calibration, so only a chosen point is real.
 *
 * The image fills its own box exactly, so there is no letterbox space and a
 * percentage position stays put when the image changes display size.
 */
export function SiteMapPinPicker({ siteMap, pin, onPinChange }: SiteMapPinPickerProps) {
  const [isUnavailable, setIsUnavailable] = useState(false)

  function placePin(event: MouseEvent<HTMLDivElement>) {
    const area = event.currentTarget.getBoundingClientRect()

    onPinChange({
      x: clamp((event.clientX - area.left) / area.width),
      y: clamp((event.clientY - area.top) / area.height),
    })
  }

  function movePin(event: KeyboardEvent<HTMLDivElement>) {
    const step = KEY_STEPS[event.key]

    if (step === undefined) {
      return
    }

    event.preventDefault()
    const from = pin ?? { x: 0.5, y: 0.5 }
    onPinChange({ x: clamp(from.x + step.x), y: clamp(from.y + step.y) })
  }

  if (siteMap === null || isUnavailable) {
    return (
      <p className="rounded-[12px] border border-dashed border-input bg-[var(--bg)] p-[18px] text-center text-[12px] text-muted-foreground">
        No site image is available for this project. Enter a location name instead.
      </p>
    )
  }

  return (
    <div
      role="button"
      tabIndex={0}
      aria-label={`Place the delay on ${siteMap.name}. Use the arrow keys to move the pin.`}
      onClick={placePin}
      onKeyDown={movePin}
      className="relative w-full cursor-crosshair overflow-hidden rounded-[12px] border border-input"
    >
      <img
        src={siteMap.uri}
        alt={siteMap.name}
        onError={() => setIsUnavailable(true)}
        className="block w-full select-none"
      />
      {pin ? (
        <span
          className="pointer-events-none absolute -translate-x-1/2 -translate-y-[80%] text-[var(--accent)]"
          style={{ left: `${pin.x * 100}%`, top: `${pin.y * 100}%` }}
        >
          <Icon icon={MapPin} size={44} strokeWidth={2} className="fill-[var(--soft)]" />
        </span>
      ) : null}
    </div>
  )
}
