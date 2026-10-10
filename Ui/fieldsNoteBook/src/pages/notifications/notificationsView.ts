import { Bell, Mail, MessageSquare, type LucideIcon } from 'lucide-react'
import type { CurrentUser } from '@/domain/users'

/** A delivery channel the recipient's own UserSettings turned on. */
export interface NoticeChannel {
  id: string
  label: string
  icon: LucideIcon
}

/**
 * In-app always applies. Email and SMS follow the recipient's preferences.
 * Push notifications are outside the approved scope and have no label here.
 */
export function noticeChannelsOf(user: CurrentUser): NoticeChannel[] {
  const channels: NoticeChannel[] = [{ id: 'inApp', label: 'In app', icon: Bell }]

  if (user.emailEnabled) {
    channels.push({ id: 'email', label: 'Email', icon: Mail })
  }

  if (user.smsEnabled) {
    channels.push({ id: 'sms', label: 'SMS', icon: MessageSquare })
  }

  return channels
}

const relative = new Intl.RelativeTimeFormat('en-GB', { numeric: 'auto' })

const UNITS: { unit: Intl.RelativeTimeFormatUnit; ms: number }[] = [
  { unit: 'day', ms: 86_400_000 },
  { unit: 'hour', ms: 3_600_000 },
  { unit: 'minute', ms: 60_000 },
]

/** "4 days ago", "2 hours ago", or "now" for anything under a minute. */
export function formatRelativeTime(value: string, now: Date = new Date()): string {
  const difference = Date.parse(value) - now.getTime()
  const unit = UNITS.find((candidate) => Math.abs(difference) >= candidate.ms)

  if (unit === undefined) {
    return 'now'
  }

  return relative.format(Math.round(difference / unit.ms), unit.unit)
}
