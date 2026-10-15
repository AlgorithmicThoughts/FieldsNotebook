/** One entry in the home activity timeline, built from an ActivityEvents row. */
export interface ActivityEntry {
  /** ActivityEvents.Id. */
  id: string
  title: string
  detail: string
  /** ActivityEvents.CreatedAt in project time, with the date when it is not today. */
  timeLabel: string
  /** ActivityEvents.CreatedAt, ISO 8601 UTC. */
  createdAt: string
  /** Delays.Number that the event belongs to. */
  delayNumber: string
}

/**
 * Readable titles for the event types the screens know about.
 * ActivityEvents.EventType is an open string, so anything else falls back to a
 * neutral title rather than failing the page.
 */
const EVENT_TITLES: Record<string, string> = {
  'delay.raised': 'Delay raised',
  'delay.advanceNoticeRaised': 'Advance notice raised',
  'delay.acknowledged': 'Delay acknowledged',
  'delay.expectedReadyChanged': 'Estimate updated',
  // The database records a revised completion time as delay.readyTimeUpdated.
  'delay.readyTimeUpdated': 'Estimate updated',
  'delay.readyForHandover': 'Submitted for review',
  'delay.handoverCheckRequested': 'Submitted for review',
  'delay.handoverVerified': 'Proof of work accepted',
  'delay.handoverReturned': 'Sent back for rework',
  'delay.resolved': 'Delay closed',
  'delay.reopened': 'Delay reopened',
  'delay.revoked': 'Delay withdrawn',
}

const NEUTRAL_TITLE = 'Delay updated'

/** The title for one ActivityEvents.EventType. */
export function describeActivityEvent(eventType: string): string {
  return EVENT_TITLES[eventType] ?? NEUTRAL_TITLE
}
