import { useEffect, useState } from 'react'
import { isAbortError, isApiError } from '@/api/errors'
import { getSiteMapByProjectId } from '@/api/siteMaps'
import type { SiteMapOption } from '@/domain/delays'

export type ProjectSiteMapState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; siteMap: SiteMapOption | null }

const GENERIC_ERROR = 'Field Notebook could not load the site image for this project.'

/** `projectId` records which project produced the result, so a late answer for
 * another project is ignored rather than rendered. */
type LoadedSiteMap =
  | { projectId: number; status: 'error'; message: string }
  | { projectId: number; status: 'ready'; siteMap: SiteMapOption | null }

/**
 * The active project's site image, read for the sample site-layout preview.
 * The request is aborted when the project changes or the preview closes.
 */
export function useProjectSiteMap(projectId: number): ProjectSiteMapState {
  const [loaded, setLoaded] = useState<LoadedSiteMap | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    getSiteMapByProjectId(projectId, { signal })
      .then((siteMap) => {
        if (!signal.aborted) {
          setLoaded({ projectId, status: 'ready', siteMap })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setLoaded({
          projectId,
          status: 'error',
          message: isApiError(error) ? error.message : GENERIC_ERROR,
        })
      })

    return () => controller.abort()
  }, [projectId])

  if (loaded === null || loaded.projectId !== projectId) {
    return { status: 'loading' }
  }

  return loaded
}
