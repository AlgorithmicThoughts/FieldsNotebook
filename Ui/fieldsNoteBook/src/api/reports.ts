import type { DelayReportRequest, DelayReportRow } from '@/domain/reports'
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
  PROJECTS,
  SEGMENTS,
  USERS,
} from './fixtures'
import type { DelayCycleRow, DelayRow, RequestOptions } from './types'

/** An upper bound no fixture reaches, so an omitted date filters nothing out. */
const OPEN_UPPER_BOUND = '9999-12-31'

function nameOrThrow(row: { Name: string } | undefined, label: string): string {
  if (row === undefined) {
    throw new ApiError(500, `Field Notebook could not read the ${label} for this delay.`)
  }

  return row.Name
}

/** Cycles oldest first. The last one carries the delay's closure, if any. */
function cyclesOf(delayId: number): DelayCycleRow[] {
  return DELAY_CYCLES.filter((cycle) => cycle.DelayId === delayId).sort(
    (left, right) => left.DelayCycleId - right.DelayCycleId,
  )
}

function crewNameOf(crewId: number): string {
  return nameOrThrow(
    CREWS.find((crew) => crew.Id === crewId),
    'crew',
  )
}

function segmentNameOf(segmentId: number | null): string | null {
  if (segmentId === null) {
    return null
  }

  return nameOrThrow(
    SEGMENTS.find((segment) => segment.Id === segmentId),
    'segment',
  )
}

function reportedByOf(userId: number): string {
  const user = USERS.find((candidate) => candidate.Id === userId)

  if (user === undefined) {
    throw new ApiError(500, 'Field Notebook could not read the author for this delay.')
  }

  return user.DisplayName
}

function projectNameOf(projectId: number): string {
  const project = PROJECTS.find((candidate) => candidate.Id === projectId)

  if (project === undefined) {
    throw new ApiError(404, 'That project is not available in Field Notebook.')
  }

  return project.Name
}

/**
 * The same checks the API enforces. A report without a project would expose
 * every project's delays, so the empty array fails rather than widening scope.
 */
function validateRequest(request: DelayReportRequest): void {
  if (request.projectIds.length === 0) {
    throw new ApiError(400, 'A delay report needs at least one project.')
  }

  for (const projectId of request.projectIds) {
    projectNameOf(projectId)
  }
}

/** Delays.CreatedAt is ISO 8601 UTC, so its date part is the comparable day. */
function withinRange(row: DelayRow, request: DelayReportRequest): boolean {
  const day = row.CreatedAt.slice(0, 10)

  return day >= (request.dateFrom ?? '') && day <= (request.dateTo ?? OPEN_UPPER_BOUND)
}

function toReportRow(row: DelayRow): DelayReportRow {
  const cycles = cyclesOf(row.Id)

  return {
    delayNumber: row.Number,
    projectName: projectNameOf(row.ProjectId),
    title: row.Title,
    statusName: nameOrThrow(
      DELAY_STATUSES.find((status) => status.Id === row.DelayStatusId),
      'status',
    ),
    tagName: nameOrThrow(
      DELAY_TAGS.find((tag) => tag.Id === row.DelayTagId),
      'reason tag',
    ),
    reportType: row.ReportType,
    ownerCrewName: crewNameOf(row.OwnerCrewId),
    blockedCrewNames: DELAY_IMPACTS.filter((impact) => impact.DelayId === row.Id).map((impact) =>
      crewNameOf(impact.BlockedCrewId),
    ),
    segmentName: segmentNameOf(row.SegmentId),
    locationName: row.LocationName,
    reportedByDisplayName: reportedByOf(row.ReportedByUserId),
    createdAt: row.CreatedAt,
    resolvedAt: cycles.at(-1)?.ClosedAt ?? null,
    cycleCount: cycles.length,
    photoCount: DELAY_PHOTOS.filter((photo) => photo.DelayId === row.Id).length,
  }
}

/**
 * Every delay the approved reporting scope covers, newest first. The request
 * carries only the project IDs and the optional date bounds.
 */
export async function getDelayReport(
  request: DelayReportRequest,
  options?: RequestOptions,
): Promise<DelayReportRow[]> {
  return mockRequest(options, () => {
    validateRequest(request)

    return DELAYS.filter(
      (row) => request.projectIds.includes(row.ProjectId) && withinRange(row, request),
    )
      .map(toReportRow)
      .sort((left, right) => right.createdAt.localeCompare(left.createdAt))
  })
}

/**
 * The approved reason tag names, in DelayTags order. A breakdown lists every
 * one of them, so a reason nobody raised reads as a deliberate zero rather
 * than a missing row. This is reference data, not part of the report payload.
 */
export async function getDelayTags(options?: RequestOptions): Promise<string[]> {
  return mockRequest(options, () => DELAY_TAGS.filter((tag) => tag.IsActive).map((tag) => tag.Name))
}
