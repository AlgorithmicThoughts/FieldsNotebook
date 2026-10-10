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
  segmentName: string
  /** The crew that cannot start, from DelayImpacts.BlockedCrewId. */
  blockedCrewName: string
  /** WorkItems.PlannedStartAt. ISO 8601 UTC. */
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
