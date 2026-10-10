import type { DelaySummary } from '@/domain/delays'

/** GET /api/projects/{projectId}/home — every count and row comes from the same set of active delays. */
export interface ProjectHome {
  metrics: ProjectHomeMetrics
  attentionDelays: DelaySummary[]
  advanceNotice: AdvanceNoticeDto | null
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

export function projectHomePath(projectId: number): string {
  return `/api/projects/${projectId}/home`
}
