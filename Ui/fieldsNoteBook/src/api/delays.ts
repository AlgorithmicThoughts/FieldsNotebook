import {
  DELAY_PHOTO_PURPOSE_ORDER,
  type DelayCycleDetail,
  type DelayDetail,
  type DelayEventDetail,
  type DelayImpactDetail,
  type DelayImpactSummary,
  type DelayPhotoDetail,
  type DelayPhotoGroup,
  type DelayPin,
  type DelayStatusCode,
  type DelaySummary,
  type DelayTagCode,
  type DelayVerification,
} from '@/domain/delays'
import { mockRequest } from './client'
import { ApiError } from './errors'
import {
  CREWS,
  DELAYS,
  DELAY_CYCLES,
  DELAY_IMPACTS,
  DELAY_IMPACT_VERIFICATIONS,
  DELAY_PHOTOS,
  DELAY_STATUSES,
  DELAY_TAGS,
  PROJECTS,
  SEGMENTS,
  USERS,
  WORK_ITEMS,
} from './fixtures'
import { ACTIVITY_EVENTS } from './fixtures/activityEvents'
import type {
  DelayCycleRow,
  DelayPhotoRow,
  DelayRow,
  ProjectRow,
  RequestOptions,
} from './types'

function requireRow<T>(row: T | undefined, label: string): T {
  if (row === undefined) {
    throw new ApiError(500, `Field Notebook could not read the ${label} for this delay.`)
  }

  return row
}

/** Cycles oldest first. The last one is the cycle the delay is living in. */
function cyclesOf(delayId: number): DelayCycleRow[] {
  return DELAY_CYCLES.filter((cycle) => cycle.DelayId === delayId).sort(
    (left, right) => left.DelayCycleId - right.DelayCycleId,
  )
}

function statusCodeOf(row: DelayRow): DelayStatusCode {
  return requireRow(
    DELAY_STATUSES.find((status) => status.Id === row.DelayStatusId),
    'status',
  ).Code
}

function tagOf(row: DelayRow): { code: DelayTagCode; name: string } {
  const tag = requireRow(
    DELAY_TAGS.find((candidate) => candidate.Id === row.DelayTagId),
    'reason tag',
  )

  return { code: tag.Code, name: tag.Name }
}

function crewNameOf(crewId: number): string {
  return requireRow(
    CREWS.find((crew) => crew.Id === crewId),
    'crew',
  ).Name
}

/** Every affected crew with the work item it cannot start. Never reduced to one. */
function impactSummariesOf(delayId: number): DelayImpactSummary[] {
  return DELAY_IMPACTS.filter((impact) => impact.DelayId === delayId).map((impact) => ({
    id: impact.Id,
    crewName: crewNameOf(impact.BlockedCrewId),
    workItemId: impact.BlockedWorkItemId,
  }))
}

function segmentNameOf(segmentId: number | null): string | null {
  if (segmentId === null) {
    return null
  }

  return requireRow(
    SEGMENTS.find((segment) => segment.Id === segmentId),
    'segment',
  ).Name
}

function pinOf(row: DelayRow): DelayPin | null {
  if (row.PinX === null || row.PinY === null) {
    return null
  }

  return { x: row.PinX, y: row.PinY }
}

function toSummary(row: DelayRow, projectName: string): DelaySummary {
  const cycles = cyclesOf(row.Id)
  const activeCycle = requireRow(cycles.at(-1), 'cycle')
  const tag = tagOf(row)
  const segmentName = segmentNameOf(row.SegmentId)
  const impacts = impactSummariesOf(row.Id)

  return {
    id: row.Id,
    number: row.Number,
    title: row.Title,
    statusCode: statusCodeOf(row),
    tagCode: tag.code,
    tagName: tag.name,
    ownerCrewName: crewNameOf(row.OwnerCrewId),
    blockedCrewNames: impacts.map((impact) => impact.crewName),
    impacts,
    locationName: row.LocationName ?? segmentName ?? 'Location not recorded',
    projectId: row.ProjectId,
    projectName,
    // A delay is High priority while it sits in a reopened cycle that is still
    // open. The database has no priority column and must not grow one here.
    isHighPriority: cycles.length > 1 && activeCycle.ClosedAt === null,
    reportType: row.ReportType,
    createdAt: row.CreatedAt,
    expectedReadyAt: activeCycle.ExpectedReadyAt,
    pin: pinOf(row),
    siteMapId: row.SiteMapId,
    segmentName,
  }
}

function projectOrThrow(projectId: number): ProjectRow {
  const project = PROJECTS.find((candidate) => candidate.Id === projectId)

  if (project === undefined) {
    throw new ApiError(404, 'That project is not available in Field Notebook.')
  }

  return project
}

/** Every delay in one project, with crew, tag, segment, and status resolved. */
export async function getDelaysByProjectId(
  projectId: number,
  options?: RequestOptions,
): Promise<DelaySummary[]> {
  return mockRequest(options, () => {
    const project = projectOrThrow(projectId)

    return DELAYS.filter((row) => row.ProjectId === projectId).map((row) =>
      toSummary(row, project.Name),
    )
  })
}

function userNameOf(userId: number): string {
  return requireRow(
    USERS.find((candidate) => candidate.Id === userId),
    'user',
  ).DisplayName
}

function workItemTitleOf(workItemId: number | null): string | null {
  if (workItemId === null) {
    return null
  }

  return WORK_ITEMS.find((row) => row.Id === workItemId)?.Title ?? null
}

/** Cycle one is the initial report. Every later cycle came from a reopen. */
function toCycleDetail(row: DelayCycleRow, index: number): DelayCycleDetail {
  return {
    id: row.DelayCycleId,
    sequence: index + 1,
    createdAt: row.CreatedAt,
    openReason: row.OpenReason,
    acknowledgmentChoice: row.AcknowledgmentChoice,
    acknowledgmentNote: row.AcknowledgmentNote,
    acknowledgedAt: row.AcknowledgedAt,
    acknowledgedByName:
      row.AcknowledgedByUserId === null ? null : userNameOf(row.AcknowledgedByUserId),
    expectedReadyAt: row.ExpectedReadyAt,
    closedAt: row.ClosedAt,
  }
}

/** Checks for one impact in one cycle. A reopened cycle starts with none. */
function verificationsOf(impactId: number, cycleId: number): DelayVerification[] {
  return DELAY_IMPACT_VERIFICATIONS.filter(
    (row) => row.DelayImpactId === impactId && row.DelayCycleId === cycleId,
  ).map((row) => ({
    verifiedByName: userNameOf(row.VerifiedByUserId),
    verifiedAt: row.VerifiedAt,
    note: row.VerificationNote,
  }))
}

/** Every affected crew. The list is never reduced to one. */
function impactsOf(delayId: number, currentCycleId: number): DelayImpactDetail[] {
  return impactSummariesOf(delayId).map((impact) => ({
    ...impact,
    workItemTitle: workItemTitleOf(impact.workItemId),
    verifications: verificationsOf(impact.id, currentCycleId),
  }))
}

function toPhotoDetail(row: DelayPhotoRow): DelayPhotoDetail {
  return {
    id: row.Id,
    originalUrl: row.OriginalUrl,
    thumbnailUrl: row.ThumbnailUrl,
    fileName: row.FileName,
    createdAt: row.CreatedAt,
    latitude: row.Latitude,
    longitude: row.Longitude,
  }
}

function photoGroupsOf(delayId: number): DelayPhotoGroup[] {
  const rows = DELAY_PHOTOS.filter((row) => row.DelayId === delayId)

  return DELAY_PHOTO_PURPOSE_ORDER.map((purpose) => ({
    purpose,
    photos: rows.filter((row) => row.Purpose === purpose).map(toPhotoDetail),
  })).filter((group) => group.photos.length > 0)
}

/** Event history, oldest first. Nothing in it is ever overwritten. */
function eventsOf(delayId: number, cycles: DelayCycleDetail[]): DelayEventDetail[] {
  return ACTIVITY_EVENTS.filter((row) => row.DelayId === delayId)
    .sort((left, right) => Date.parse(left.CreatedAt) - Date.parse(right.CreatedAt))
    .map((row) => ({
      id: row.Id,
      eventType: row.EventType,
      actorName: userNameOf(row.ActorUserId),
      createdAt: row.CreatedAt,
      cycleSequence: cycles.find((cycle) => cycle.id === row.DelayCycleId)?.sequence ?? null,
      oldExpectedReadyAt: row.OldExpectedReadyAt,
      newExpectedReadyAt: row.NewExpectedReadyAt,
    }))
}

/**
 * One delay with its author, every cycle, every affected crew with that
 * crew's checks for the current cycle, its photos, and its event history.
 */
export async function getDelayById(
  delayId: number,
  options?: RequestOptions,
): Promise<DelayDetail> {
  return mockRequest(options, () => {
    const row = DELAYS.find((candidate) => candidate.Id === delayId)

    if (row === undefined) {
      throw new ApiError(404, 'That delay is not available in Field Notebook.')
    }

    const project = projectOrThrow(row.ProjectId)
    const cycles = cyclesOf(row.Id).map(toCycleDetail)
    const currentCycle = requireRow(cycles.at(-1), 'cycle')

    return {
      ...toSummary(row, project.Name),
      description: row.Description,
      reportedBy: {
        userId: row.ReportedByUserId,
        displayName: userNameOf(row.ReportedByUserId),
        crewName: crewNameOf(row.ReportedByCrewId),
        reportedAt: row.CreatedAt,
      },
      cycles,
      photos: photoGroupsOf(row.Id),
      impacts: impactsOf(row.Id, currentCycle.id),
      events: eventsOf(row.Id, cycles),
    }
  })
}
