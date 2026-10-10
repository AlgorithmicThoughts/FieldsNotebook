import { useEffect, useState } from 'react'
import { getTrackingDetail } from '@/api/tracking'
import { isAbortError, isApiError } from '@/api/errors'
import type { DelayDetail, SiteMapOption } from '@/domain/delays'

const GENERIC_ERROR = 'Field Notebook could not load this delay.'

export interface DelayDetailData {
  delay: DelayDetail
  /** The project's site image, so a pinned delay can show its position. */
  siteMap: SiteMapOption | null
}

/**
 * `key` records the request the result belongs to. A late answer for a
 * previously opened delay no longer matches and never renders.
 */
export type DelayDetailState =
  | { key: null }
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; data: DelayDetailData }

const PENDING: DelayDetailState = { key: null }

/** Loads one delay and the site image its pin is normalised against. */
export function useDelayDetail(
  delayId: number | null,
  projectId: number | null,
): DelayDetailState {
  const [state, setState] = useState<DelayDetailState>(PENDING)
  const requestKey = delayId === null || projectId === null ? null : `${delayId}:${projectId}`

  useEffect(() => {
    if (requestKey === null || delayId === null || projectId === null) {
      return
    }

    const controller = new AbortController()
    const { signal } = controller

    getTrackingDetail(delayId, projectId, { signal })
      .then((data) => {
        if (!signal.aborted) {
          setState({ key: requestKey, status: 'ready', data })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setState({
          key: requestKey,
          status: 'error',
          message: isApiError(error) ? error.message : GENERIC_ERROR,
        })
      })

    return () => controller.abort()
  }, [delayId, projectId, requestKey])

  return state.key === requestKey ? state : PENDING
}
