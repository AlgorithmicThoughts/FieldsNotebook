import type { DelayReportRequest, DelayReportRow } from '@/domain/reports'
import { apiGet } from './client'
import type { RequestOptions } from './types'

/**
 * The request as a query string. The API reads only these three inputs, and an
 * omitted date leaves that end of the range open.
 */
function toQuery(request: DelayReportRequest): string {
  const query = new URLSearchParams()

  for (const projectId of request.projectIds) {
    query.append('projectIds', String(projectId))
  }

  if (request.dateFrom !== undefined) {
    query.set('dateFrom', request.dateFrom)
  }

  if (request.dateTo !== undefined) {
    query.set('dateTo', request.dateTo)
  }

  return query.toString()
}

/**
 * GET /api/reports/delays: every delay in the requested projects, newest
 * first. Both dates are inclusive and compare against the UTC day of
 * Delays.CreatedAt. The API answers 403 when any project is not the user's.
 */
export async function getDelayReport(
  request: DelayReportRequest,
  options?: RequestOptions,
): Promise<DelayReportRow[]> {
  return apiGet<DelayReportRow[]>(`/api/reports/delays?${toQuery(request)}`, options)
}

/**
 * GET /api/reports/delay-tags: the approved reason tag names, in DelayTags
 * order. A breakdown lists every one of them, so a reason nobody raised reads
 * as a deliberate zero rather than a missing row.
 */
export async function getDelayTags(options?: RequestOptions): Promise<string[]> {
  return apiGet<string[]>('/api/reports/delay-tags', options)
}
