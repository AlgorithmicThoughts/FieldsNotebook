import { getTrackingDetail } from '@/api/tracking'
import type { DelayDetail, SiteMapOption } from '@/domain/delays'
import { useApiQuery, type ApiQuery } from '@/hooks/useApiQuery'

const GENERIC_ERROR = 'Field Notebook could not load this delay.'

export interface DelayDetailData {
  delay: DelayDetail
  /** The project's site image, so a pinned delay can show its position. */
  siteMap: SiteMapOption | null
}

/**
 * Loads one delay and the site image its pin is normalised against. `reload`
 * asks for the delay again, which is how a workflow action shows its result
 * rather than the screen guessing the new state. The loaded delay stays on
 * screen until the fresh one arrives, so the open detail never blanks out and
 * remounts under the user.
 */
export function useDelayDetail(delayId: number | null, projectId: number | null): ApiQuery<DelayDetailData> {
  const key = delayId === null || projectId === null ? null : `delay:${projectId}:${delayId}`

  return useApiQuery(
    key,
    (signal) => getTrackingDetail(delayId ?? 0, projectId ?? 0, { signal }),
    GENERIC_ERROR,
  )
}
