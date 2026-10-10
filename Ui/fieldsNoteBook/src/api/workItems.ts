import type { WorkItemPredecessor, WorkItemSummary } from '@/domain/workItems'
import { mockRequest } from './client'
import { ApiError } from './errors'
import {
  CREWS,
  PROJECTS,
  SEGMENTS,
  WORK_ITEMS,
  WORK_ITEM_DEPENDENCIES,
  WORK_ITEM_TEAM_ASSIGNMENTS,
} from './fixtures'
import type { RequestOptions, WorkItemRow } from './types'

function crewNamesOf(workItemId: number): string[] {
  return WORK_ITEM_TEAM_ASSIGNMENTS.filter((row) => row.WorkItemId === workItemId)
    .map((row) => CREWS.find((crew) => crew.Id === row.CrewId)?.Name)
    .filter((name) => name !== undefined)
}

function segmentNameOf(segmentId: number | null): string | null {
  if (segmentId === null) {
    return null
  }

  return SEGMENTS.find((segment) => segment.Id === segmentId)?.Name ?? null
}

function predecessorsOf(workItemId: number): WorkItemPredecessor[] {
  return WORK_ITEM_DEPENDENCIES.filter((row) => row.SuccessorWorkItemId === workItemId)
    .map((row) => WORK_ITEMS.find((item) => item.Id === row.PredecessorWorkItemId))
    .filter((item) => item !== undefined)
    .map((item) => ({ id: item.Id, title: item.Title }))
}

function toSummary(row: WorkItemRow): WorkItemSummary {
  return {
    id: row.Id,
    title: row.Title,
    crewNames: crewNamesOf(row.Id),
    segmentName: segmentNameOf(row.SegmentId),
    plannedStartAt: row.PlannedStartAt,
    plannedFinishAt: row.PlannedFinishAt,
    expectedFinishAt: row.ExpectedFinishAt,
    isCompleted: row.IsCompleted,
    predecessors: predecessorsOf(row.Id),
  }
}

/**
 * Every planned task in one project, with crews, segment, and predecessors
 * resolved. Completed work is included; the caller decides what its screen
 * shows, and the Upcoming page leaves completed work out.
 */
export async function getWorkItemsByProjectId(
  projectId: number,
  options?: RequestOptions,
): Promise<WorkItemSummary[]> {
  return mockRequest(options, () => {
    if (!PROJECTS.some((project) => project.Id === projectId)) {
      throw new ApiError(404, 'That project is not available in Field Notebook.')
    }

    return WORK_ITEMS.filter((row) => row.ProjectId === projectId).map(toSummary)
  })
}
