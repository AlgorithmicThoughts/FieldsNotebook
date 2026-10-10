import {
  deriveDelayTitle,
  type CreateDelayRequest,
  type DelayFormOptions,
  type DelayPin,
  type DelayStatusCode,
  type DelaySummary,
  type DelayTagCode,
  type NotifyPersonOption,
  type SiteMapOption,
  type WorkItemOption,
} from '@/domain/delays'
import { mockRequest } from './client'
import { ApiError } from './errors'
import {
  CREWS,
  DELAYS,
  DELAY_CYCLES,
  DELAY_IMPACTS,
  DELAY_PHOTOS,
  DELAY_STATUSES,
  DELAY_TAGS,
  MOCK_SIGNED_IN_USER_ID,
  PROJECTS,
  ROLES,
  SEGMENTS,
  SITE_MAPS,
  USERS,
  USER_PROJECTS,
  USER_ROLES,
  WORK_ITEMS,
  WORK_ITEM_TEAM_ASSIGNMENTS,
} from './fixtures'
import type { DelayCycleRow, DelayRow, ProjectRow, RequestOptions } from './types'

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

function blockedCrewNamesOf(delayId: number): string[] {
  return DELAY_IMPACTS.filter((impact) => impact.DelayId === delayId).map((impact) =>
    crewNameOf(impact.BlockedCrewId),
  )
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

  return {
    id: row.Id,
    number: row.Number,
    title: row.Title,
    statusCode: statusCodeOf(row),
    tagCode: tag.code,
    tagName: tag.name,
    ownerCrewName: crewNameOf(row.OwnerCrewId),
    blockedCrewNames: blockedCrewNamesOf(row.Id),
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

function workItemsOf(projectId: number): WorkItemOption[] {
  return WORK_ITEMS.filter((row) => row.ProjectId === projectId && !row.IsCompleted).map((row) => ({
    id: row.Id,
    title: row.Title,
    crewIds: WORK_ITEM_TEAM_ASSIGNMENTS.filter(
      (assignment) => assignment.WorkItemId === row.Id,
    ).map((assignment) => assignment.CrewId),
  }))
}

function siteMapOf(projectId: number): SiteMapOption | null {
  const row = SITE_MAPS.find((candidate) => candidate.ProjectId === projectId && candidate.IsActive)

  if (row === undefined) {
    return null
  }

  return { id: row.Id, name: row.Name, uri: row.Uri }
}

/** The crew a user leads or supervises in one project, which is at most one. */
function crewOf(userId: number, projectId: number) {
  return CREWS.find(
    (candidate) =>
      candidate.ProjectId === projectId &&
      (candidate.ForemanLeadId === userId || candidate.SuperintendentUserId === userId),
  )
}

function toNotifyPerson(userId: number, projectId: number): NotifyPersonOption | null {
  const user = USERS.find((candidate) => candidate.Id === userId && candidate.IsActive)
  const userRole = USER_ROLES.find(
    (row) => row.UserId === userId && row.ProjectId === projectId && row.IsActive,
  )
  const role = userRole && ROLES.find((candidate) => candidate.Id === userRole.RoleId)
  const crew = crewOf(userId, projectId)

  if (!user || !role || !crew) {
    return null
  }

  return {
    userId,
    displayName: user.DisplayName,
    crewId: crew.Id,
    crewName: crew.Name,
    roleName: role.Name,
  }
}

function peopleOf(projectId: number): NotifyPersonOption[] {
  return USER_PROJECTS.filter((row) => row.ProjectId === projectId && row.IsActive)
    .map((row) => toNotifyPerson(row.UserId, projectId))
    .filter((person) => person !== null)
}

/** The crews, work items, segments, reasons, map, and people one project offers. */
export async function getDelayFormOptions(
  projectId: number,
  options?: RequestOptions,
): Promise<DelayFormOptions> {
  return mockRequest(options, () => {
    const project = projectOrThrow(projectId)

    return {
      projectId,
      projectName: project.Name,
      crews: CREWS.filter((row) => row.ProjectId === projectId && row.IsActive).map((row) => ({
        id: row.Id,
        name: row.Name,
      })),
      workItems: workItemsOf(projectId),
      segments: SEGMENTS.filter((row) => row.ProjectId === projectId && row.IsActive).map(
        (row) => ({ id: row.Id, name: row.Name }),
      ),
      tags: DELAY_TAGS.filter((row) => row.IsActive).map((row) => ({
        id: row.Id,
        code: row.Code,
        name: row.Name,
      })),
      siteMap: siteMapOf(projectId),
      people: peopleOf(projectId),
    }
  })
}

/** Stand-in for the address the real API returns after it stores the upload. */
const PHOTO_PLACEHOLDER_URL = 'https://placehold.co/400x400/png'

function nextId(rows: { Id: number }[]): number {
  return rows.reduce((highest, row) => Math.max(highest, row.Id), 0) + 1
}

/** Delays.Number reads FN-#### and is unique across the database. */
function nextDelayNumber(): string {
  const highest = DELAYS.reduce(
    (value, row) => Math.max(value, Number.parseInt(row.Number.slice(3), 10)),
    0,
  )

  return `FN-${highest + 1}`
}

/**
 * The same checks the API enforces. Repeating them here keeps the mock honest;
 * the API remains the authority.
 */
function validateCreateRequest(request: CreateDelayRequest): void {
  if (request.impacts.length === 0) {
    throw new ApiError(400, 'A delay must name at least one affected crew.')
  }

  if (request.photos.length < 1 || request.photos.length > 5) {
    throw new ApiError(400, 'A delay report needs one to five photos.')
  }
}

function appendDelayRow(request: CreateDelayRequest, createdAt: string): DelayRow {
  const reporterCrew = crewOf(MOCK_SIGNED_IN_USER_ID, request.projectId)
  const row: DelayRow = {
    Id: nextId(DELAYS),
    Number: nextDelayNumber(),
    ProjectId: request.projectId,
    Title: deriveDelayTitle(request.description),
    Description: request.description,
    ReportedByUserId: MOCK_SIGNED_IN_USER_ID,
    ReportedByCrewId: reporterCrew?.Id ?? request.ownerCrewId,
    OwnerCrewId: request.ownerCrewId,
    DelayTagId: request.delayTagId,
    ReportType: request.reportType,
    BlockingWorkItemId: request.blockingWorkItemId,
    // A new report always starts at Raised. Status 1 is that row.
    DelayStatusId: 1,
    SegmentId: request.segmentId,
    SiteMapId: request.siteMapId,
    PinX: request.pin?.x ?? null,
    PinY: request.pin?.y ?? null,
    LocationSource: request.locationSource,
    LocationName: request.locationName,
    CreatedAt: createdAt,
  }

  DELAYS.push(row)
  return row
}

function appendCycleRow(delayId: number, request: CreateDelayRequest, createdAt: string): number {
  const cycleId =
    DELAY_CYCLES.reduce((highest, row) => Math.max(highest, row.DelayCycleId), 0) + 1

  DELAY_CYCLES.push({
    DelayCycleId: cycleId,
    DelayId: delayId,
    CreatedAt: createdAt,
    CreatedBy: MOCK_SIGNED_IN_USER_ID,
    ModifiedAt: null,
    ModifiedBy: null,
    OpenReason: null,
    AcknowledgmentChoice: null,
    AcknowledgedByUserId: null,
    AcknowledgedAt: null,
    AcknowledgmentNote: null,
    // Tentative from the start. A time alone never clears the blocker.
    ExpectedReadyAt: request.expectedReadyAt,
    ClosedByUserId: null,
    ClosedAt: null,
  })

  return cycleId
}

function appendImpactRows(delayId: number, request: CreateDelayRequest): void {
  for (const impact of request.impacts) {
    DELAY_IMPACTS.push({
      Id: nextId(DELAY_IMPACTS),
      DelayId: delayId,
      ProjectId: request.projectId,
      BlockedCrewId: impact.blockedCrewId,
      BlockedWorkItemId: impact.blockedWorkItemId,
    })
  }
}

function appendPhotoRows(
  delayId: number,
  cycleId: number,
  request: CreateDelayRequest,
  createdAt: string,
): void {
  for (const photo of request.photos) {
    DELAY_PHOTOS.push({
      Id: nextId(DELAY_PHOTOS),
      DelayId: delayId,
      DelayCycleId: cycleId,
      Purpose: 'InitialReport',
      CreatedByUserId: MOCK_SIGNED_IN_USER_ID,
      UpdatedByUserId: null,
      OriginalUrl: PHOTO_PLACEHOLDER_URL,
      ThumbnailUrl: PHOTO_PLACEHOLDER_URL,
      FileName: photo.file.name,
      CreatedAt: createdAt,
      UpdatedAt: null,
      Latitude: photo.latitude,
      Longitude: photo.longitude,
    })
  }
}

/**
 * Raises a delay. The real API writes the delay, its first cycle, one impact
 * row per affected crew, the photos, and the recipients' notifications in a
 * single transaction, and it uploads each file rather than taking a URL.
 */
export async function createDelay(
  request: CreateDelayRequest,
  options?: RequestOptions,
): Promise<DelaySummary> {
  return mockRequest(options, () => {
    const project = projectOrThrow(request.projectId)
    validateCreateRequest(request)

    const createdAt = new Date().toISOString()
    const row = appendDelayRow(request, createdAt)
    const cycleId = appendCycleRow(row.Id, request, createdAt)

    appendImpactRows(row.Id, request)
    appendPhotoRows(row.Id, cycleId, request, createdAt)

    return toSummary(row, project.Name)
  })
}
