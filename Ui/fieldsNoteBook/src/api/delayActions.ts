import type { DelayAcknowledgmentChoice, DelayPhotoUpload, DelayStatusCode } from '@/domain/delays'
import { apiPost, apiPostForm, apiPostJson } from './client'
import { appendPhotos } from './delayReports'
import type { RequestOptions } from './types'

/**
 * The API's answer to one workflow action: where the delay now stands and the
 * history it appended. The full delay comes from a fresh tracking request, so
 * the screen reloads rather than patching its own copy.
 */
export interface DelayActionResult {
  delayId: number
  statusCode: DelayStatusCode
  cycleId: number
  activityEventId: number
  /** ActivityEvents.EventType, such as delay.acknowledged. */
  eventType: string
  occurredAt: string
  /** Notifications rows written for this event, one per recipient. */
  noticeCount: number
}

function actionPath(projectId: number, delayId: number, action: string): string {
  return `/api/projects/${projectId}/delays/${delayId}/${action}`
}

/** The responsible Superintendent answers the delay. */
export function acknowledgeDelay(
  projectId: number,
  delayId: number,
  body: { choice: DelayAcknowledgmentChoice; note: string | null; expectedReadyAt: string | null },
  options?: RequestOptions,
): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'acknowledgment'), body, options)
}

/** A revised tentative completion time. It updates the active cycle, never a new one. */
export function reviseCompletionTime(
  projectId: number,
  delayId: number,
  expectedReadyAt: string,
  options?: RequestOptions,
): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'completion-time'), { expectedReadyAt }, options)
}

/** The responsible Superintendent asks the affected crews' Foremen for a check. */
export function requestHandoverCheck(
  projectId: number,
  delayId: number,
  options?: RequestOptions,
): Promise<DelayActionResult> {
  return apiPost(actionPath(projectId, delayId, 'handover-check'), options)
}

/** The affected crew's Foreman records a successful check for one impact. */
export function verifyHandover(
  projectId: number,
  delayId: number,
  body: { impactId: number; note: string | null },
  options?: RequestOptions,
): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'verifications'), body, options)
}

/**
 * The responsible Superintendent closes the cycle after the required checks,
 * attaching one to five proof-of-work photos.
 */
export async function resolveDelay(
  projectId: number,
  delayId: number,
  photos: DelayPhotoUpload[],
  options?: RequestOptions,
): Promise<DelayActionResult> {
  const form = new FormData()
  await appendPhotos(form, photos)
  return apiPostForm(actionPath(projectId, delayId, 'resolution'), form, options)
}

/** A Superintendent reopens a resolved delay, which opens a new cycle. */
export function reopenDelay(
  projectId: number,
  delayId: number,
  reason: string,
  options?: RequestOptions,
): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'reopen'), { reason }, options)
}

/** The Superintendent of the reporting Foreman withdraws the delay. */
export function revokeDelay(
  projectId: number,
  delayId: number,
  options?: RequestOptions,
): Promise<DelayActionResult> {
  return apiPost(actionPath(projectId, delayId, 'revocation'), options)
}
