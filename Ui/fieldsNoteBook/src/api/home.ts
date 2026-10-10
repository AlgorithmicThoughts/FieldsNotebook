import { describeActivityEvent, type ActivityEntry } from '@/domain/activity'
import type { DelaySummary } from '@/domain/delays'
import { formatProjectEventTime } from '@/lib/projectTime'
import { apiGet } from './client'
import type { RequestOptions } from './types'

/** Counts across every active delay for the user's crews, not only the rows shown. */
export interface HomeMetricCounts {
  blockingOurCrews: number
  crewsWaitingOnUs: number
  readyToVerify: number
}

/** Everything Home shows, from one request. */
export interface ProjectHome {
  metrics: HomeMetricCounts
  /** The most pressing active delays for the user's crews, at most four. */
  attentionDelays: DelaySummary[]
  /** The advance notice the hero announces, or null. */
  advanceNotice: DelaySummary | null
  recentActivity: ActivityEntry[]
}

/** One workflow event as GET /home returns it. */
interface ActivityEntryResponse {
  id: number
  eventType: string
  delayNumber: string
  delayTitle: string
  actorName: string
  /** ISO 8601 UTC. */
  createdAt: string
}

interface ProjectHomeResponse extends Omit<ProjectHome, 'recentActivity'> {
  recentActivity: ActivityEntryResponse[]
}

function toEntry(event: ActivityEntryResponse, now: Date): ActivityEntry {
  return {
    id: String(event.id),
    title: describeActivityEvent(event.eventType),
    detail: [event.actorName, event.delayTitle].filter(Boolean).join(' · '),
    timeLabel: formatProjectEventTime(event.createdAt, now),
    createdAt: event.createdAt,
    delayNumber: event.delayNumber,
  }
}

/**
 * GET /api/projects/{projectId}/home. The API applies the delay rules (which
 * delays count, which are pressing, which notice leads), so Home needs one call.
 */
export async function getProjectHome(projectId: number, options?: RequestOptions): Promise<ProjectHome> {
  const home = await apiGet<ProjectHomeResponse>(`/api/projects/${projectId}/home`, options)

  const now = new Date()

  return { ...home, recentActivity: home.recentActivity.map((event) => toEntry(event, now)) }
}
