/** One entry in the home activity timeline, built from an ActivityEvents row. */
export interface ActivityEntry {
  /** ActivityEvents.Id. */
  id: string
  title: string
  detail: string
  /** ActivityEvents.CreatedAt in the project's local time. */
  timeLabel: string
  /** Delays.Number that the event belongs to. */
  delayNumber: string
}
