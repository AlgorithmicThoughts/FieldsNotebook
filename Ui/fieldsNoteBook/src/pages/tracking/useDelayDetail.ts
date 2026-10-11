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
 * `key` records the delay the result belongs to. A late answer for a
 * previously opened delay no longer matches and never renders. A reload of the
 * same delay keeps the same key, so the loaded delay stays on screen while the
 * fresh one arrives.
 */
export type DelayDetailState =
  | { key: null }
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; data: DelayDetailData }

const PENDING: DelayDetailState = { key: null }

/**
 * Loads one delay and the site image its pin is normalised against. Raising
 * `reloadCount` asks for the delay again, which is how a workflow action shows
 * its result rather than the screen guessing the new state. The reload replaces
 * the delay once the answer arrives, so the open detail never blanks out and
 * remounts under the user.
 */
export function useDelayDetail(
  delayId: number | null,
  projectId: number | null,
  reloadCount = 0,
): DelayDetailState {
  const [state, setState] = useState<DelayDetailState>(PENDING)
  const delayKey = delayId === null || projectId === null ? null : `${delayId}:${projectId}`

  useEffect(() => {
    if (delayKey === null || delayId === null || projectId === null) {
      return
    }

    const controller = new AbortController()
    const { signal } = controller

    getTrackingDetail(delayId, projectId, { signal })
      .then((data) => {
        if (!signal.aborted) {
          setState({ key: delayKey, status: 'ready', data })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setState({
          key: delayKey,
          status: 'error',
          message: isApiError(error) ? error.message : GENERIC_ERROR,
        })
      })

    return () => controller.abort()
  }, [delayId, delayKey, projectId, reloadCount])

  return state.key === delayKey ? state : PENDING
}
