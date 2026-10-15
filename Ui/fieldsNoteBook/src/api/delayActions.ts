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

/** Each action creates a sub-resource of the delay, such as an acknowledgment or a handover request. */
function actionPath(projectId: number, delayId: number, action: string): string {
  return `/api/projects/${projectId}/delays/${delayId}/${action}`
}

/** The responsible Superintendent answers the delay. */
export function acknowledgeDelay(projectId: number, delayId: number, body: { choice: DelayAcknowledgmentChoice; note: string | null; expectedReadyAt: string | null }, options?: RequestOptions): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'acknowledgments'), body, options)
}

/** A revised tentative completion time. It updates the active cycle, never a new one. */
export function reviseCompletionTime(projectId: number, delayId: number, expectedReadyAt: string, options?: RequestOptions): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'completion-time-revisions'), { expectedReadyAt }, options)
}

/**
 * The responsible crew's Superintendent or Foreman submits the delay for review,
 * attaching one to five proof-of-work photos the affected crews look at.
 */
export async function requestHandoverCheck(projectId: number, delayId: number, photos: DelayPhotoUpload[], options?: RequestOptions): Promise<DelayActionResult> {
  const form = new FormData()
  await appendPhotos(form, photos)
  return apiPostForm(actionPath(projectId, delayId, 'handover-requests'), form, options)
}

/**
 * An affected crew's Foreman accepts the proof of work for their own crew. The
 * last crew to accept closes the delay.
 */
export function verifyHandover(projectId: number, delayId: number, body: { impactId: number; note: string | null }, options?: RequestOptions): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, `impacts/${body.impactId}/verifications`), { note: body.note }, options)
}

/** An affected crew's Foreman sends the proof of work back, saying what to fix. */
export function returnHandover(projectId: number, delayId: number, body: { impactId: number; reason: string }, options?: RequestOptions): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, `impacts/${body.impactId}/returns`), { reason: body.reason }, options)
}

/** A Superintendent reopens a resolved delay, which opens a new cycle. */
export function reopenDelay(projectId: number, delayId: number, reason: string, options?: RequestOptions): Promise<DelayActionResult> {
  return apiPostJson(actionPath(projectId, delayId, 'cycles'), { reason }, options)
}

/** The Superintendent of the reporting Foreman withdraws the delay. */
export function revokeDelay(projectId: number, delayId: number, options?: RequestOptions): Promise<DelayActionResult> {
  return apiPost(actionPath(projectId, delayId, 'revocations'), options)
}
