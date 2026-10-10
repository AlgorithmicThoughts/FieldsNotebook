import type { DelaySummary } from '@/domain/delays'

/** GET /api/projects/{projectId}/home — every count and row comes from the same set of active delays. */
export interface ProjectHome {
  metrics: ProjectHomeMetrics
  attentionDelays: DelaySummary[]
  advanceNotice: AdvanceNoticeDto | null
  /** Newest events first, at most five. */
  recentActivity: ActivityEntryDto[]
}

export interface ProjectHomeMetrics {
  blockingOurCrews: number
  crewsWaitingOnUs: number
  readyToVerify: number
}

export interface AdvanceNoticeDto {
  delayNumber: string
  title: string
  description: string
  ownerCrewName: string
  blockedCrewNames: string[]
  /** UTC ISO timestamp; null until the responsible Superintendent gives an estimate. */
  expectedReadyAt: string | null
}

/** One workflow event; the client writes the timeline text. */
export interface ActivityEntryDto {
  id: number
  /** ActivityEvents.EventType, such as delay.reopened. */
  eventType: string
  delayNumber: string
  delayTitle: string
  actorName: string
  fromStatusCode: string | null
  toStatusCode: string | null
  /** UTC ISO timestamp of a new tentative completion time. */
  newExpectedReadyAt: string | null
  /** The affected crew of a handover check. */
  affectedCrewName: string | null
  /** UTC ISO timestamp. */
  createdAt: string
}

export function projectHomePath(projectId: number): string {
  return `/api/projects/${projectId}/home`
}
