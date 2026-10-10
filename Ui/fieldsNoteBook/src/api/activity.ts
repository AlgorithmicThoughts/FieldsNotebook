import type { ActivityEntry } from '@/domain/activity'
import { formatProjectTime } from '@/lib/projectTime'
import { mockRequest } from './client'
import { ACTIVITY_EVENTS } from './fixtures/activityEvents'
import { DELAYS, USERS } from './fixtures'
import type { ActivityEventRow, RequestOptions } from './types'

/**
 * Readable titles for the event types the screens know about. The column is an
 * open string, so an unrecognised type falls back to a neutral title instead
 * of failing the page.
 */
const EVENT_TITLES: Record<string, string> = {
  'delay.raised': 'Delay raised',
  'delay.advanceNoticeRaised': 'Advance notice raised',
  'delay.acknowledged': 'Delay acknowledged',
  'delay.expectedReadyChanged': 'Estimate updated',
  'delay.readyForHandover': 'Ready for verification',
  'delay.handoverCheckRequested': 'Handover check requested',
  'delay.handoverVerified': 'Handover verified',
  'delay.resolved': 'Delay resolved',
  'delay.reopened': 'Delay reopened',
  'delay.revoked': 'Delay withdrawn',
}

const NEUTRAL_TITLE = 'Delay updated'

function toEntry(row: ActivityEventRow): ActivityEntry {
  const delay = DELAYS.find((candidate) => candidate.Id === row.DelayId)
  const actor = USERS.find((candidate) => candidate.Id === row.ActorUserId)

  return {
    id: String(row.Id),
    title: EVENT_TITLES[row.EventType] ?? NEUTRAL_TITLE,
    detail: [actor?.DisplayName, delay?.Title].filter(Boolean).join(' · '),
    timeLabel: formatProjectTime(row.CreatedAt),
    delayNumber: delay?.Number ?? '',
  }
}

/** The most recent activity in one project, newest first. */
export async function getRecentActivityByProjectId(
  projectId: number,
  limit: number,
  options?: RequestOptions,
): Promise<ActivityEntry[]> {
  return mockRequest(options, () =>
    ACTIVITY_EVENTS.filter((row) => row.ProjectId === projectId)
      .sort((left, right) => Date.parse(right.CreatedAt) - Date.parse(left.CreatedAt))
      .slice(0, limit)
      .map(toEntry),
  )
}
