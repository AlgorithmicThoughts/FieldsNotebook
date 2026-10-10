/** GET /api/projects/{projectId}/notifications/unread-count */
export interface UnreadNotificationCount {
  unreadCount: number
}

export function unreadNotificationCountPath(projectId: number): string {
  return `/api/projects/${projectId}/notifications/unread-count`
}
