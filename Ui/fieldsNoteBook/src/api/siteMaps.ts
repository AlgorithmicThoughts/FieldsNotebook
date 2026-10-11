import type { SiteMapOption } from '@/domain/delays'
import { mockRequest } from './client'
import { SITE_MAPS } from './fixtures'
import type { RequestOptions } from './types'

/** The photo a delay summary shows beside its pin. */
export interface DelayPhotoPreview {
  /** DelayPhotos.DelayId. */
  delayId: number
  /** DelayPhotos.OriginalUrl. */
  originalUrl: string
  /** DelayPhotos.ThumbnailUrl, within 400 by 400 pixels. */
  thumbnailUrl: string
  fileName: string
  /** DelayPhotos.CreatedAt, ISO 8601 UTC. */
  createdAt: string
}

/**
 * The active site image for one project, or null when the project has none.
 * A project has at most one active map; multi-map support is not approved.
 */
export async function getSiteMapByProjectId(
  projectId: number,
  options?: RequestOptions,
): Promise<SiteMapOption | null> {
  return mockRequest(options, () => {
    const row = SITE_MAPS.find(
      (candidate) => candidate.ProjectId === projectId && candidate.IsActive,
    )

    return row === undefined ? null : { id: row.Id, name: row.Name, uri: row.Uri }
  })
}
