/** A work item this one waits on, from WorkItemDependencies. */
export interface WorkItemPredecessor {
  /** WorkItems.Id of the predecessor. */
  id: number
  title: string
}

/** One planned task, with its crews, segment, and predecessors resolved. */
export interface WorkItemSummary {
  /** WorkItems.Id. */
  id: number
  title: string
  /**
   * Every crew WorkItemTeamAssignment records. The assignment is many to one,
   * so a work item can be planned for more than one crew.
   */
  crewNames: string[]
  /** Segments.Name, or null when the work item sits outside a segment. */
  segmentName: string | null
  /** ISO 8601 UTC. Format at the edge, never in the DTO. */
  plannedStartAt: string | null
  plannedFinishAt: string | null
  /**
   * WorkItems.ExpectedFinishAt. The plan's expected finish for this task. It
   * is not a Superintendent's acknowledged completion time and never binding.
   */
  expectedFinishAt: string | null
  isCompleted: boolean
  /** The work items that must hand over before this one can start. */
  predecessors: WorkItemPredecessor[]
}
