import { describeActivityEvent, type ActivityEntry } from '@/domain/activity'
import { formatProjectTime } from '@/lib/projectTime'
import { mockRequest } from './client'
import { ACTIVITY_EVENTS } from './fixtures/activityEvents'
import { DELAYS, USERS } from './fixtures'
import type { ActivityEventRow, RequestOptions } from './types'

function toEntry(row: ActivityEventRow): ActivityEntry {
  const delay = DELAYS.find((candidate) => candidate.Id === row.DelayId)
  const actor = USERS.find((candidate) => candidate.Id === row.ActorUserId)

  return {
    id: String(row.Id),
    title: describeActivityEvent(row.EventType),
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
