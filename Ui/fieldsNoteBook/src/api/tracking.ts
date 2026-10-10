import type { DelayDetail, DelaySummary, SiteMapOption } from '@/domain/delays'
import { apiGet } from './client'
import { ApiError } from './errors'
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

export async function getTrackingDetail(delayId: number, projectId: number, options?: RequestOptions) {
  const tracking = await getProjectTracking(projectId, options)
  const row = tracking.delays.find((candidate) => candidate.summary.id === delayId)
  if (row === undefined) {
    throw new ApiError(404, 'That delay is not available in Field Notebook.')
  }
  const { summary, siteMap, ...detail } = row
  return { delay: { ...summary, ...detail } satisfies DelayDetail, siteMap }
}