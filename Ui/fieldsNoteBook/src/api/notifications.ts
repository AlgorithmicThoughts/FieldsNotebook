import { describeActivityEvent } from '@/domain/activity'
import type { NotificationSummary } from '@/domain/notifications'
import { apiGet, apiPost } from './client'
import type { RequestOptions } from './types'

/** One notice as the notifications endpoints return it. */
interface NotificationResponse {
  id: number
  delayId: number
  delayNumber: string
  delayTitle: string
  eventType: string
  createdAt: string
  readAt: string | null
}

/** The recipient's notices with the unread total that the shell's badge shows. */
export interface ProjectNotices {
  unreadCount: number
  notices: NotificationSummary[]
}

/** The opened notice with the recipient's new unread total, so the badge needs no second request. */
export interface NoticeRead {
  notice: NotificationSummary
  unreadCount: number
}

/**
 * The event supplies the readable sentence. An event type the client does not
 * know yet still reads as a sentence rather than a dotted string.
 */
function toSummary(response: NotificationResponse, projectName: string): NotificationSummary {
  return {
    id: response.id,
    delayId: response.delayId,
    delayNumber: response.delayNumber,
    delayTitle: response.delayTitle,
    projectName,
    title: describeActivityEvent(response.eventType),
    eventType: response.eventType,
    createdAt: response.createdAt,
    readAt: response.readAt,
    isRead: response.readAt !== null,
  }
}

/**
 * GET /api/projects/{projectId}/notifications — the signed-in user's notices
 * in one project, newest first, with the unread total from the same request.
 * Reading the list marks nothing read.
 */
export async function getProjectNotices(
  projectId: number,
  projectName: string,
  options?: RequestOptions,
): Promise<ProjectNotices> {
  const response = await apiGet<{ unreadCount: number; notices: NotificationResponse[] }>(
    `/api/projects/${projectId}/notifications`,
    options,
  )

  return {
    unreadCount: response.unreadCount,
    notices: response.notices.map((notice) => toSummary(notice, projectName)),
  }
}

/**
 * POST /api/projects/{projectId}/notifications/{notificationId}/read — records
 * that this recipient opened this notice. The API takes the recipient from the
 * signed-in identity, and an already read notice keeps its first read time.
 */
export async function markNotificationRead(
  projectId: number,
  projectName: string,
  notificationId: number,
  options?: RequestOptions,
): Promise<NoticeRead> {
  const response = await apiPost<{ notice: NotificationResponse; unreadCount: number }>(
    `/api/projects/${projectId}/notifications/${notificationId}/read`,
    options,
  )

  return { notice: toSummary(response.notice, projectName), unreadCount: response.unreadCount }
}
