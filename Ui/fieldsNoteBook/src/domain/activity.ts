import type { ActivityEntryDto } from '@/api/home'
import { formatRelativeDayLabel, formatTimeLabel } from './time'

/** Timeline titles for ActivityEvents.EventType codes. */
const EVENT_TITLES: Record<string, string> = {
  'delay.raised': 'Delay raised',
  'delay.acknowledged': 'Delay acknowledged',
  'delay.readyTimeUpdated': 'Completion time updated',
  'delay.readyForHandover': 'Ready for handover',
  'delay.handoverCheckRequested': 'Handover check requested',
  'delay.handoverVerified': 'Handover verified',
  'delay.resolved': 'Delay resolved',
  'delay.reopened': 'Delay reopened',
  'delay.revoked': 'Delay revoked',
}

export function toActivityTitle(entry: ActivityEntryDto): string {
  return EVENT_TITLES[entry.eventType] ?? 'Delay updated'
}

/** One sentence about who did what; a time is an estimate, never clearance. */
export function toActivityDetail(entry: ActivityEntryDto, now: Date): string {
  if (entry.newExpectedReadyAt) {
    const readyAt = new Date(entry.newExpectedReadyAt)
    return `${entry.actorName} estimates ${formatTimeLabel(readyAt)}, ${formatRelativeDayLabel(readyAt, now)}.`
  }
  if (entry.affectedCrewName) {
    return `${entry.affectedCrewName} · ${entry.actorName}`
  }
  return `${entry.delayTitle} · ${entry.actorName}`
}
