import type { LucideIcon } from 'lucide-react'
import { Badge as ShadcnBadge } from '@/components/ui/badge'
import type { StatusTone } from '@/domain/delays'
import { Icon } from './Icon'

/** Status tones plus the neutral outline used for informational labels. */
export type BadgeTone = StatusTone | 'outline'

/** Approved badge metrics: 6 px radius, 11 px text, and 13 px icons. */
const APPROVED_SHAPE =
  'h-auto gap-[5px] rounded-[6px] px-2 py-[5px] text-[11px] leading-[1.4] font-[750] [&>svg]:size-[13px]!'

const TONE_SHAPE: Record<BadgeTone, string> = {
  neutral: 'bg-[var(--disabled)] text-[var(--muted)]',
  blocked: 'bg-[var(--blocked-bg)] text-[var(--blocked)]',
  progress: 'bg-[var(--progress-bg)] text-[var(--progress)]',
  verify: 'bg-[var(--verify-bg)] text-[var(--verify)]',
  resolved: 'bg-[var(--resolved-bg)] text-[var(--resolved)]',
  urgent: 'bg-[var(--urgent-bg)] text-[var(--urgent)]',
  outline: 'border-border text-[var(--muted)]',
}

interface BadgeProps {
  label: string
  tone?: BadgeTone
  icon?: LucideIcon
}

/** A status label. Colour always appears with text and an icon. */
export function Badge({ label, tone = 'neutral', icon }: BadgeProps) {
  return (
    <ShadcnBadge
      variant={tone === 'outline' ? 'outline' : 'secondary'}
      className={`${APPROVED_SHAPE} ${TONE_SHAPE[tone]}`}
    >
      {icon ? <Icon icon={icon} size={13} /> : null}
      {label}
    </ShadcnBadge>
  )
}
