import { describeActivityEvent } from '@/domain/activity'
import type { NotificationSummary } from '@/domain/notifications'
import { apiGet, mockRequest } from './client'
import { ApiError } from './errors'
import { DELAYS, NOTIFICATIONS, PROJECTS } from './fixtures'
import { ACTIVITY_EVENTS } from './fixtures/activityEvents'
import type { NotificationRow, RequestOptions } from './types'

/**
 * The event supplies the readable sentence. An event type the client does not
 * know yet still reads as a sentence rather than a dotted string.
 */
function toSummary(row: NotificationRow): NotificationSummary {
  const event = ACTIVITY_EVENTS.find((candidate) => candidate.Id === row.ActivityEventId)
  const delay = DELAYS.find((candidate) => candidate.Id === row.DelayId)
  const project = PROJECTS.find((candidate) => candidate.Id === row.ProjectId)

  if (event === undefined || delay === undefined || project === undefined) {
    throw new ApiError(500, 'Field Notebook could not read the delay behind a notice.')
  }

  return {
    id: row.Id,
    delayId: row.DelayId,
    delayNumber: delay.Number,
    delayTitle: delay.Title,
    projectName: project.Name,
    title: describeActivityEvent(event.EventType),
    eventType: event.EventType,
    createdAt: row.CreatedAt,
    readAt: row.ReadAt,
    isRead: row.ReadAt !== null,
  }
}

/**
 * The signed-in user's notices in one project, newest first. The scope is one
 * recipient and one project; a notice is never shared with another user.
 */
export async function getNotificationsByUserId(
  userId: number,
  projectId: number,
  options?: RequestOptions,
): Promise<NotificationSummary[]> {
  return mockRequest(options, () =>
    NOTIFICATIONS.filter((row) => row.UserId === userId && row.ProjectId === projectId)
      .sort((left, right) => Date.parse(right.CreatedAt) - Date.parse(left.CreatedAt))
      .map(toSummary),
  )
}

/**
 * Records that this recipient opened this notice. The real API takes the
 * recipient from the token, so a client cannot read someone else's notice.
 * An already read notice keeps its first read time.
 */
export async function markNotificationRead(
  notificationId: number,
  options?: RequestOptions,
): Promise<NotificationSummary> {
  return mockRequest(options, () => {
    const row = NOTIFICATIONS.find((candidate) => candidate.Id === notificationId)

    if (row === undefined) {
      throw new ApiError(404, 'That notice is not available in Field Notebook.')
    }

    row.ReadAt = row.ReadAt ?? new Date().toISOString()

    return toSummary(row)
  })
}

/**
 * GET /api/projects/{projectId}/notifications/unread-count — the signed-in
 * user's unopened notices in the project. Reading the count marks nothing read.
 */
export async function getUnreadNotificationCount(projectId: number, options?: RequestOptions): Promise<number> {
  const { unreadCount } = await apiGet<{ unreadCount: number }>(
    `/api/projects/${projectId}/notifications/unread-count`,
    options,
  )

  return unreadCount
}
