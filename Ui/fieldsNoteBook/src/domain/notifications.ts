/**
 * One Notifications row with its event and delay resolved. A notice belongs to
 * exactly one recipient, and reading it grants no workflow permission.
 */
export interface NotificationSummary {
  /** Notifications.Id. */
  id: number
  delayId: number
  /** Delays.Number, such as FN-1042. */
  delayNumber: string
  delayTitle: string
  projectName: string
  /** Readable sentence derived from ActivityEvents.EventType. */
  title: string
  /** Open dotted string such as `delay.raised`. */
  eventType: string
  /** ISO 8601 UTC. Format at the edge, never in the DTO. */
  createdAt: string
  /** Notifications.ReadAt, set only when this recipient opened this notice. */
  readAt: string | null
  isRead: boolean
}
