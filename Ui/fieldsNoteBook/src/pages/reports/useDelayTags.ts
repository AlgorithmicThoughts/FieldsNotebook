import { getDelayTags } from '@/api/reports'
import { useApiQuery } from '@/hooks/useApiQuery'
import type { TagState } from './reportsView'

const TAGS_FAILED = 'Field Notebook could not load the delay reasons.'

/**
 * Loads the approved reason tags once for the page. `retry` loads them again
 * only after a failure; a loaded list is never refetched for a new scope.
 */
export function useDelayTags(): { tags: TagState; retry: () => void } {
  const { state, reload } = useApiQuery('delay-tags', (signal) => getDelayTags({ signal }), TAGS_FAILED)

  function retry() {
    if (state.status === 'error') {
      reload()
    }
  }

  if (state.status === 'ready') {
    return { tags: { status: 'ready', tagNames: state.data }, retry }
  }

  return { tags: state.status === 'error' ? state : { status: 'loading' }, retry }
}
