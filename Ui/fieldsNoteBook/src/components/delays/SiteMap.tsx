import { useRef, useState, type KeyboardEvent, type MouseEvent, type ReactNode } from 'react'
import { MapPin } from 'lucide-react'
import {
  DELAY_STATUS_DISPLAY,
  type DelayPin,
  type DelayStatusCode,
  type StatusTone,
} from '@/domain/delays'
import { Icon } from '@/components/common/Icon'

/** One arrow press moves the draft pin two percent of the image. */
const PIN_STEP = 0.02

const KEY_STEPS: Record<string, DelayPin> = {
  ArrowUp: { x: 0, y: -PIN_STEP },
  ArrowDown: { x: 0, y: PIN_STEP },
  ArrowLeft: { x: -PIN_STEP, y: 0 },
  ArrowRight: { x: PIN_STEP, y: 0 },
}

/** The status tokens carry no shadcn role, so a pin addresses them directly. */
const PIN_COLOR: Record<StatusTone, string> = {
  neutral: 'var(--muted)',
  blocked: 'var(--blocked)',
  progress: 'var(--progress)',
  verify: 'var(--verify)',
  resolved: 'var(--resolved)',
  urgent: 'var(--urgent)',
}

/** Approved map pin metrics. A pin is the one place the 20 px icon does not apply. */
const MARKER_SHAPE =
  'absolute grid h-12 w-11 -translate-x-1/2 -translate-y-[80%] place-items-center border-0 bg-transparent p-0 drop-shadow-[0_3px_3px_var(--shadow)]'

const MARKER_LABEL_SHAPE = 'absolute top-[15px] text-[8px] leading-none font-[850] text-[var(--text)]'

const SURFACE_SHAPE = 'relative w-full overflow-hidden bg-[var(--bg)]'

/** Prototype placeholder until site images come from Azure blob storage. */
const SITE_IMAGE_URL = '/sitemap.png'

/** One delay that carries a pin on the project's site image. */
export interface SiteMapPin {
  /** Delays.Number, such as FN-1042. */
  delayNumber: string
  title: string
  statusCode: DelayStatusCode
  /** Normalised 0-1 position against the image's own content box. */
  pin: DelayPin
}

/** Pin coordinates run from zero to one inside the image. */
function clamp(value: number): number {
  return Math.min(1, Math.max(0, value))
}

function positionOf(pin: DelayPin) {
  return { left: `${pin.x * 100}%`, top: `${pin.y * 100}%` }
}

/** `FN-1042` reads as `1042` inside the pin head, as the approved design shows. */
function shortNumberOf(delayNumber: string): string {
  return delayNumber.replace(/^FN-/, '')
}

interface MarkerGlyphProps {
  label: string
  isHighlighted: boolean
}

function MarkerGlyph({ label, isHighlighted }: MarkerGlyphProps) {
  return (
    <>
      <Icon
        icon={MapPin}
        size={44}
        strokeWidth={isHighlighted ? 2.7 : 2}
        className={
          isHighlighted ? 'fill-[var(--soft)] stroke-[var(--accent)]' : 'fill-[var(--surface)]'
        }
      />
      <span className={MARKER_LABEL_SHAPE}>{label}</span>
    </>
  )
}

interface PinMarkerProps {
  item: SiteMapPin
  isSelected: boolean
  onSelect?: (delayNumber: string) => void
}

/**
 * A pin is a real button whenever the caller can act on it, so keyboard users
 * reach it. Without a handler it stays a plain marker rather than a dead
 * control, which is also what the editable picker needs.
 */
function PinMarker({ item, isSelected, onSelect }: PinMarkerProps) {
  const display = DELAY_STATUS_DISPLAY[item.statusCode]
  const style = { ...positionOf(item.pin), color: PIN_COLOR[display.tone] }
  const glyph = <MarkerGlyph label={shortNumberOf(item.delayNumber)} isHighlighted={isSelected} />

  if (onSelect === undefined) {
    return (
      <span style={style} className={MARKER_SHAPE}>
        {glyph}
      </span>
    )
  }

  return (
    <button
      type="button"
      aria-label={`${item.delayNumber}: ${item.title} · ${display.label}`}
      aria-pressed={isSelected}
      style={style}
      className={`${MARKER_SHAPE} cursor-pointer hover:[&_svg]:fill-[var(--soft)]`}
      onClick={() => onSelect(item.delayNumber)}
    >
      {glyph}
    </button>
  )
}

interface SiteMapProps {
  imageName: string
  /** Delays carrying a pin. */
  pins: SiteMapPin[]
  selectedDelayNumber?: string
  onSelectPin?: (delayNumber: string) => void
  /** Editable turns the image into a picker for a draft pin. */
  editable?: boolean
  draftPin?: DelayPin | null
  onPlaceDraftPin?: (pin: DelayPin) => void
  compact?: boolean
}

/**
 * The project site image with a pin per delay, shared by the site map page,
 * the report form's picker, and a compact read-only view.
 *
 * The image keeps its natural proportions and fills the surface exactly, so
 * there is no letterbox space and a percentage position stays correct when the
 * image changes display size. Nothing here derives a position from photo GPS:
 * the image has no calibration, so only a point a user chose is real.
 */
export function SiteMap({
  imageName,
  pins,
  selectedDelayNumber,
  onSelectPin,
  editable = false,
  draftPin = null,
  onPlaceDraftPin,
  compact = false,
}: SiteMapProps) {
  const imageRef = useRef<HTMLImageElement>(null)
  const [isUnavailable, setIsUnavailable] = useState(false)

  function placeDraftPin(event: MouseEvent<HTMLDivElement>) {
    const area = imageRef.current?.getBoundingClientRect()

    if (area === undefined || onPlaceDraftPin === undefined) {
      return
    }

    onPlaceDraftPin({
      x: clamp((event.clientX - area.left) / area.width),
      y: clamp((event.clientY - area.top) / area.height),
    })
  }

  function moveDraftPin(event: KeyboardEvent<HTMLDivElement>) {
    const step = KEY_STEPS[event.key]

    if (step === undefined || onPlaceDraftPin === undefined) {
      return
    }

    event.preventDefault()
    const from = draftPin ?? { x: 0.5, y: 0.5 }
    onPlaceDraftPin({ x: clamp(from.x + step.x), y: clamp(from.y + step.y) })
  }

  if (isUnavailable) {
    return (
      <p className="rounded-[12px] border border-dashed border-border bg-[var(--bg)] p-[18px] text-center text-[12px] text-muted-foreground">
        The site image for {imageName} did not load.
      </p>
    )
  }

  const contents: ReactNode = (
    <>
      <img
        ref={imageRef}
        src={SITE_IMAGE_URL}
        alt={imageName}
        draggable={false}
        onError={() => setIsUnavailable(true)}
        className="block w-full select-none"
      />
      <span
        className={`pointer-events-none absolute bottom-3 left-[14px] rounded-[5px] bg-card px-[7px] py-1 text-muted-foreground ${compact ? 'text-[9px]' : 'text-[10px]'}`}
      >
        Sample map · Not for layout
      </span>
      {pins.map((item) => (
        <PinMarker
          key={item.delayNumber}
          item={item}
          isSelected={item.delayNumber === selectedDelayNumber}
          // A pin inside the picker would nest a button in a button.
          onSelect={editable ? undefined : onSelectPin}
        />
      ))}
      {draftPin ? (
        <span
          className={`${MARKER_SHAPE} pointer-events-none text-[var(--accent)]`}
          style={positionOf(draftPin)}
        >
          <MarkerGlyph label="NEW" isHighlighted />
        </span>
      ) : null}
    </>
  )

  if (editable) {
    return (
      <div
        role="button"
        tabIndex={0}
        aria-label={`Place the delay on ${imageName}. Use the arrow keys to move the pin.`}
        onClick={placeDraftPin}
        onKeyDown={moveDraftPin}
        className={`${SURFACE_SHAPE} cursor-crosshair rounded-[12px] border border-[var(--control)]`}
      >
        {contents}
      </div>
    )
  }

  return (
    <div className={`${SURFACE_SHAPE} ${compact ? 'rounded-[12px] border border-border' : ''}`}>
      {contents}
    </div>
  )
}
