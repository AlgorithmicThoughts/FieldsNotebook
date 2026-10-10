import type { CreateDelayRequest, DelayLocationSource, DelayPin } from '@/domain/delays'
import { coversCrewId, type CurrentUser } from '@/domain/users'
import { canReportDelay } from '@/domain/permissions'

export const MIN_PHOTOS = 1
export const MAX_PHOTOS = 5
export const DESCRIPTION_MAX_LENGTH = 2000
export const LOCATION_NAME_MAX_LENGTH = 160
export const TITLE_MAX_LENGTH = 200

/** One affected crew and the work item it cannot start. */
export interface AffectedCrewDraft {
  crewId: number
  workItemId: number | null
}

/** One chosen photo, with its preview and whatever GPS the file carried. */
export interface PhotoDraft {
  id: string
  file: File
  previewUrl: string
  latitude: number | null
  longitude: number | null
}

export interface DelayDraft {
  title: string
  description: string
  ownerCrewId: number | null
  affectedCrews: AffectedCrewDraft[]
  blockingWorkItemId: number | null
  delayTagId: number | null
  /** Reported before the affected crew arrives, so ReportType is AdvanceNotice. */
  isAdvanceNotice: boolean
  photos: PhotoDraft[]
  segmentId: number | null
  pin: DelayPin | null
  locationName: string
  notifyUserIds: number[]
}

/** Each report owns fresh values, with only the reporter's crew preselected. */
export function createDelayDraft(user: CurrentUser): DelayDraft {
  return {
    title: '',
    description: '',
    ownerCrewId: null,
    affectedCrews: user.crews.map((crew) => ({ crewId: crew.id, workItemId: null })),
    blockingWorkItemId: null,
    delayTagId: null,
    isAdvanceNotice: false,
    photos: [],
    segmentId: null,
    pin: null,
    locationName: '',
    notifyUserIds: [],
  }
}

export type DelayDraftField =
  | 'title'
  | 'description'
  | 'ownerCrew'
  | 'affectedCrews'
  | 'reason'
  | 'photos'
  | 'location'
  | 'segment'

export type DelayDraftErrors = Partial<Record<DelayDraftField, string>>

/** GPS is optional, so a pin or a typed name has to stand in for it. */
function hasLocation(draft: DelayDraft): boolean {
  return (
    draft.pin !== null ||
    draft.locationName.trim().length > 0 ||
    draft.photos.some((photo) => photo.latitude !== null)
  )
}

interface DraftRule {
  field: DelayDraftField
  message: string
  isBroken: (draft: DelayDraft) => boolean
}

const DRAFT_RULES: DraftRule[] = [
  {
    field: 'title',
    message: 'Enter a delay title.',
    isBroken: (draft) => draft.title.trim().length === 0,
  },
  {
    field: 'segment',
    message: 'Choose the segment where the delay occurs.',
    isBroken: (draft) => draft.segmentId === null,
  },
  {
    field: 'description',
    message: 'Describe what stops the work.',
    isBroken: (draft) => draft.description.trim().length === 0,
  },
  {
    field: 'ownerCrew',
    message: 'Choose the crew that must remove the blocker.',
    isBroken: (draft) => draft.ownerCrewId === null,
  },
  {
    field: 'affectedCrews',
    message: 'Choose at least one affected crew.',
    isBroken: (draft) => draft.affectedCrews.length === 0,
  },
  {
    field: 'reason',
    message: 'Choose one reason.',
    isBroken: (draft) => draft.delayTagId === null,
  },
  {
    field: 'photos',
    message: `Add ${MIN_PHOTOS} to ${MAX_PHOTOS} photos.`,
    isBroken: (draft) => draft.photos.length < MIN_PHOTOS || draft.photos.length > MAX_PHOTOS,
  },
  {
    field: 'location',
    message:
      'No photo carries GPS, so place a pin on the site map or enter a location name.',
    isBroken: (draft) => !hasLocation(draft),
  },
]

export function validateDraft(draft: DelayDraft): DelayDraftErrors {
  const errors: DelayDraftErrors = {}

  for (const rule of DRAFT_RULES) {
    if (rule.isBroken(draft)) {
      errors[rule.field] = rule.message
    }
  }

  return errors
}

/**
 * Display enforcement only. The API decides who may raise a delay; refusing
 * here saves a round trip and never grants authority the client does not have.
 */
export function reporterError(draft: DelayDraft, user: CurrentUser): string | null {
  if (!canReportDelay(user)) {
    return 'Only a Foreman reports a delay.'
  }

  const crewIds = draft.affectedCrews.map((crew) => crew.crewId)

  const namesOwnCrew =
    (draft.ownerCrewId !== null && coversCrewId(user, draft.ownerCrewId)) ||
    crewIds.some((crewId) => coversCrewId(user, crewId))

  if (draft.ownerCrewId !== null && !namesOwnCrew) {
    return 'Name your own crew as the responsible crew or as an affected crew.'
  }

  return null
}

function locationSourceOf(draft: DelayDraft): DelayLocationSource | null {
  if (draft.pin !== null) {
    return 'ManualPin'
  }

  return draft.locationName.trim().length > 0 ? 'ManualEntry' : null
}

/**
 * The request body, or null when a required choice is still missing. Callers
 * validate first; this keeps the contract honest rather than asserting.
 */
export function toCreateDelayRequest(
  draft: DelayDraft,
  projectId: number,
  siteMapId: number | null,
): CreateDelayRequest | null {
  if (draft.ownerCrewId === null || draft.delayTagId === null) {
    return null
  }

  return {
    projectId,
    title: draft.title.trim(),
    description: draft.description.trim(),
    ownerCrewId: draft.ownerCrewId,
    impacts: draft.affectedCrews.map((crew) => ({
      blockedCrewId: crew.crewId,
      blockedWorkItemId: crew.workItemId,
    })),
    blockingWorkItemId: draft.blockingWorkItemId,
    delayTagId: draft.delayTagId,
    reportType: draft.isAdvanceNotice ? 'AdvanceNotice' : 'AfterArrival',
    photos: draft.photos.map((photo) => ({
      file: photo.file,
      latitude: photo.latitude,
      longitude: photo.longitude,
    })),
    segmentId: draft.segmentId,
    siteMapId: draft.pin === null ? null : siteMapId,
    pin: draft.pin,
    locationSource: locationSourceOf(draft),
    locationName: draft.locationName.trim() || null,
    notifyUserIds: draft.notifyUserIds,
  }
}
