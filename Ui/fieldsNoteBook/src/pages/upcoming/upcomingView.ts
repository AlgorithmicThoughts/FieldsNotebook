import { isDelayActive, type DelayImpactSummary, type DelaySummary } from '@/domain/delays'
import type { WorkItemSummary } from '@/domain/workItems'
import { formatProjectDayLabel, projectDaysBetween } from '@/lib/projectTime'

const DAY_MS = 86_400_000

/** "Tomorrow · 11 October" for the project day this page plans for. */
export function nextProjectDayLabel(now: Date = new Date()): string {
  return formatProjectDayLabel(new Date(now.getTime() + DAY_MS), now)
}

/**
 * Which timestamp the row shows as the tentative completion. The two sources
 * are different things, so the row says which one it is reading.
 */
export type CompletionEstimate =
  | { source: 'none' }
  /** DelayCycles.ExpectedReadyAt for the delay's active cycle. */
  | { source: 'delayCycle'; at: string }
  /** WorkItems.ExpectedFinishAt on a predecessor, which nobody acknowledged. */
  | { source: 'workItem'; at: string; workItemTitle: string }

/** A predecessor that an unresolved delay still sits on. */
export interface BlockedPredecessor {
  title: string
  delayNumber: string
}

/** One blocked work item. One delay with several impacts produces several rows. */
export interface BlockedWorkRow {
  /** DelayImpacts.Id, which is unique per affected crew and work item. */
  id: number
  workItemTitle: string
  segmentName: string | null
  /** The crew that cannot start, from DelayImpacts.BlockedCrewId. */
  blockedCrewName: string
  /** WorkItems.PlannedStartAt. */
  plannedStartAt: string
  delayNumber: string
  delayTitle: string
  /** The crew that must remove the blocker. */
  ownerCrewName: string
  tagName: string
  isAdvanceNotice: boolean
  isHighPriority: boolean
  completion: CompletionEstimate
  blockedPredecessor: BlockedPredecessor | null
}

/**
 * Work that should be under way by the end of the next project day and is not
 * finished. Work planned for an earlier day stays in the list while it is
 * unresolved, because the rules keep every open blocker visible for the next
 * day's plan.
 */
function isPlannedByNextDay(workItem: WorkItemSummary, now: Date): boolean {
  if (workItem.isCompleted || workItem.plannedStartAt === null) {
    return false
  }

  return projectDaysBetween(now, workItem.plannedStartAt) <= 1
}

function completionOf(
  delay: DelaySummary,
  workItem: WorkItemSummary,
  byId: Map<number, WorkItemSummary>,
): CompletionEstimate {
  if (delay.expectedReadyAt !== null) {
    return { source: 'delayCycle', at: delay.expectedReadyAt }
  }

  for (const predecessor of workItem.predecessors) {
    const expectedFinishAt = byId.get(predecessor.id)?.expectedFinishAt

    if (expectedFinishAt) {
      return { source: 'workItem', at: expectedFinishAt, workItemTitle: predecessor.title }
    }
  }

  return { source: 'none' }
}

/** The predecessor a Foreman actually asks about: the one still held up. */
function blockedPredecessorOf(
  workItem: WorkItemSummary,
  activeDelays: DelaySummary[],
): BlockedPredecessor | null {
  for (const predecessor of workItem.predecessors) {
    const blocker = activeDelays.find((delay) =>
      delay.impacts.some((impact) => impact.workItemId === predecessor.id),
    )

    if (blocker !== undefined) {
      return { title: predecessor.title, delayNumber: blocker.number }
    }
  }

  return null
}

interface RowSource {
  delay: DelaySummary
  impact: DelayImpactSummary
  workItem: WorkItemSummary
  activeDelays: DelaySummary[]
  byId: Map<number, WorkItemSummary>
}

function toRow({ delay, impact, workItem, activeDelays, byId }: RowSource): BlockedWorkRow {
  return {
    id: impact.id,
    workItemTitle: workItem.title,
    segmentName: workItem.segmentName,
    blockedCrewName: impact.crewName,
    plannedStartAt: workItem.plannedStartAt ?? '',
    delayNumber: delay.number,
    delayTitle: delay.title,
    ownerCrewName: delay.ownerCrewName,
    tagName: delay.tagName,
    isAdvanceNotice: delay.reportType === 'AdvanceNotice',
    isHighPriority: delay.isHighPriority,
    completion: completionOf(delay, workItem, byId),
    blockedPredecessor: blockedPredecessorOf(workItem, activeDelays),
  }
}

function byPlannedStart(left: BlockedWorkRow, right: BlockedWorkRow): number {
  return Date.parse(left.plannedStartAt) - Date.parse(right.plannedStartAt)
}

/**
 * Every unresolved blocker on the next project day's work, one row per
 * affected crew and work item. A resolved or withdrawn delay drops out; a
 * completion time that has passed does not, because only the workflow clears
 * a blocker.
 */
export function toBlockedWork(
  delays: DelaySummary[],
  workItems: WorkItemSummary[],
  now: Date,
): BlockedWorkRow[] {
  const activeDelays = delays.filter(isDelayActive)
  const byId = new Map(workItems.map((workItem) => [workItem.id, workItem]))
  const rows: BlockedWorkRow[] = []

  for (const delay of activeDelays) {
    for (const impact of delay.impacts) {
      const workItem = impact.workItemId === null ? undefined : byId.get(impact.workItemId)

      if (workItem !== undefined && isPlannedByNextDay(workItem, now)) {
        rows.push(toRow({ delay, impact, workItem, activeDelays, byId }))
      }
    }
  }

  return rows.sort(byPlannedStart)
}
