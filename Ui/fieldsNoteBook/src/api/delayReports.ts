import type {
  CreateDelayRequest,
  DelayFormOptions,
  DelayPhotoUpload,
  DelaySpeechResponse,
  DelaySummary,
} from '@/domain/delays'
import { createPhotoThumbnail } from '@/lib/photoThumbnail'
import { apiGet, apiPostForm } from './client'
import { ApiError } from './errors'
import type { RequestOptions } from './types'

/** One upfront request supplies all the report form's project options. */
export function getDelayFormOptions(projectId: number, options?: RequestOptions) {
  return apiGet<DelayFormOptions>(`/api/projects/${projectId}/delays/report-options`, options)
}

export function transcribeDelay(projectId: number, audio: Blob, options?: RequestOptions) {
  const form = new FormData()
  form.append('Audio', audio, 'recording.wav')
  return apiPostForm<DelaySpeechResponse>(`/api/projects/${projectId}/delays/transcriptions`, form, options)
}

function appendFields(form: FormData, fields: Record<string, unknown>) {
  for (const [key, value] of Object.entries(fields)) {
    if (value !== null && value !== undefined) form.append(key, String(value))
  }
}

async function appendPhoto(form: FormData, photo: DelayPhotoUpload, index: number) {
  let thumbnail: Blob
  try { thumbnail = await createPhotoThumbnail(photo.file) } catch {
    throw new ApiError(400, `Could not read ${photo.file.name}. Try a JPG or PNG photo.`)
  }
  // File-only items need an explicit collection index when GPS fields are absent.
  form.append('Photos.index', String(index))
  form.append(`Photos[${index}].File`, photo.file)
  form.append(`Photos[${index}].Thumbnail`, thumbnail, 'thumbnail.jpg')
  appendFields(form, {
    [`Photos[${index}].Latitude`]: photo.latitude,
    [`Photos[${index}].Longitude`]: photo.longitude,
  })
}

/**
 * Adds each photo with a JPEG thumbnail, in the multipart shape the API binds for
 * both the initial report and a resolution's proof of work.
 */
export async function appendPhotos(form: FormData, photos: DelayPhotoUpload[]) {
  for (let index = 0; index < photos.length; index++) await appendPhoto(form, photos[index], index)
}

export async function createDelay(request: CreateDelayRequest, options?: RequestOptions): Promise<DelaySummary> {
  const { photos, impacts, notifyUserIds, pin, projectId, ...fields } = request
  const form = new FormData()
  appendFields(form, { ...fields, PinX: pin?.x, PinY: pin?.y })
  impacts.forEach((impact, index) => appendFields(form, {
    [`Impacts[${index}].BlockedCrewId`]: impact.blockedCrewId,
    [`Impacts[${index}].BlockedWorkItemId`]: impact.blockedWorkItemId,
  }))
  notifyUserIds.forEach((id, index) => form.append(`NotifyUserIds[${index}]`, String(id)))
  await appendPhotos(form, photos)
  return apiPostForm<DelaySummary>(`/api/projects/${projectId}/delays`, form, options)
}
