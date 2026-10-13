import type { DelayDetail, DelaySummary, SiteMapOption } from '@/domain/delays'
import { apiGet } from './client'
import type { RequestOptions } from './types'

interface TrackingDelayResponse extends Omit<DelayDetail, keyof DelaySummary> {
  summary: DelaySummary
  impacts: DelayDetail['impacts']
  siteMap: SiteMapOption | null
}

export interface ProjectTracking {
  projectId: number
  projectName: string
  delays: TrackingDelayResponse[]
}

/** Project access and input validation belong to DelayManager. */
export function getProjectTracking(projectId: number, options?: RequestOptions): Promise<ProjectTracking> {
  return apiGet<ProjectTracking>(`/api/projects/${projectId}/delays`, options)
}

/** One delay; the API answers 404 when it is not in the project. */
export async function getTrackingDetail(delayId: number, projectId: number, options?: RequestOptions) {
  const row = await apiGet<TrackingDelayResponse>(`/api/projects/${projectId}/delays/${delayId}`, options)
  const { summary, siteMap, ...detail } = row
  return { delay: { ...summary, ...detail } satisfies DelayDetail, siteMap }
}
