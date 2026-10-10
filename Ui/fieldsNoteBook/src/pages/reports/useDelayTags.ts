import { useEffect, useState } from 'react'
import { isAbortError, isApiError } from '@/api/errors'
import { getDelayTags } from '@/api/reports'
import type { TagState } from './reportsView'

const TAGS_FAILED = 'Field Notebook could not load the delay reasons.'

/**
 * Loads the approved reason tags once for the page. `retry` loads them again
 * only after a failure; a loaded list is never refetched for a new scope.
 */
export function useDelayTags(): { tags: TagState; retry: () => void } {
  const [attempt, setAttempt] = useState(0)
  const [tags, setTags] = useState<TagState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    getDelayTags({ signal })
      .then((tagNames) => {
        if (!signal.aborted) {
          setTags({ status: 'ready', tagNames })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setTags({ status: 'error', message: isApiError(error) ? error.message : TAGS_FAILED })
      })

    return () => controller.abort()
  }, [attempt])

  function retry() {
    if (tags.status !== 'error') {
      return
    }

    setTags({ status: 'loading' })
    setAttempt((count) => count + 1)
  }

  return { tags, retry }
}
