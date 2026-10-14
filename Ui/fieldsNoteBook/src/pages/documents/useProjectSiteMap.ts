import { getSiteMapByProjectId } from '@/api/siteMaps'
import type { SiteMapOption } from '@/domain/delays'
import { useApiQuery } from '@/hooks/useApiQuery'

export type ProjectSiteMapState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; siteMap: SiteMapOption | null }

const GENERIC_ERROR = 'Field Notebook could not load the site image for this project.'

/**
 * The active project's site image, read for the sample site-layout preview.
 * The request is aborted when the project changes or the preview closes.
 */
export function useProjectSiteMap(projectId: number): ProjectSiteMapState {
  const { state } = useApiQuery(
    `site-image:${projectId}`,
    (signal) => getSiteMapByProjectId(projectId, { signal }),
    GENERIC_ERROR,
  )

  if (state.status === 'ready') {
    return { status: 'ready', siteMap: state.data }
  }

  return state.status === 'error' ? state : { status: 'loading' }
}
