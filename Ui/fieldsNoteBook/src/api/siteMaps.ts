import type { SiteMapOption } from '@/domain/delays'
import { mockRequest } from './client'
import { DELAYS, DELAY_PHOTOS, SITE_MAPS } from './fixtures'
import type { DelayPhotoRow, RequestOptions } from './types'

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

function toPreview(row: DelayPhotoRow): DelayPhotoPreview {
  return {
    delayId: row.DelayId,
    originalUrl: row.OriginalUrl,
    thumbnailUrl: row.ThumbnailUrl,
    fileName: row.FileName,
    createdAt: row.CreatedAt,
  }
}

function newestPhotoOf(rows: DelayPhotoRow[], delayId: number): DelayPhotoRow | undefined {
  return rows
    .filter((row) => row.DelayId === delayId)
    .sort((left, right) => Date.parse(right.CreatedAt) - Date.parse(left.CreatedAt))
    .at(0)
}

/**
 * The latest photo of each delay in one project, so selecting a pin shows its
 * photo without another request. The real delay detail contract will carry a
 * delay's photos; this reads the same rows until that endpoint exists.
 */
export async function getLatestDelayPhotosByProjectId(
  projectId: number,
  options?: RequestOptions,
): Promise<DelayPhotoPreview[]> {
  return mockRequest(options, () =>
    DELAYS.filter((delay) => delay.ProjectId === projectId)
      .map((delay) => newestPhotoOf(DELAY_PHOTOS, delay.Id))
      .filter((row) => row !== undefined)
      .map(toPreview),
  )
}
