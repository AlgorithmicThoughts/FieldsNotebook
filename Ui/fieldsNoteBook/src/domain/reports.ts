/**
 * The approved reporting contract, and the whole of it. External report
 * consumers see delay data only through these three inputs. A crew, status,
 * tag, grouping, sort, or paging parameter is a contract change and needs
 * approval; filtering an already-returned result set on screen does not.
 */
export interface DelayReportRequest {
  /** One or more project IDs. Required. */
  projectIds: number[]
  /** ISO date, inclusive. Omitted means every delay back to the first one. */
  dateFrom?: string
  /** ISO date, inclusive. Omitted means every delay up to the latest one. */
  dateTo?: string
}

/** One delay, flattened with every identifier resolved to a name. */
export interface DelayReportRow {
  /** Delays.Number, such as FN-1042. */
  delayNumber: string
  projectName: string
  title: string
  /** DelayStatus.Name. */
  statusName: string
  /** DelayTags.Name, the cause and never the status or the priority. */
  tagName: string
  /** Delays.ReportType: AfterArrival or AdvanceNotice. */
  reportType: string
  /** Crew that must remove the blocker. */
  ownerCrewName: string
  /** Every crew DelayImpacts records. A delay is never reduced to one crew. */
  blockedCrewNames: string[]
  segmentName: string | null
  locationName: string | null
  reportedByDisplayName: string
  /** Delays.CreatedAt, ISO 8601 UTC. */
  createdAt: string
  /** DelayCycles.ClosedAt of the final cycle, ISO 8601 UTC, or null. */
  resolvedAt: string | null
  /** More than one cycle means the delay was reopened. */
  cycleCount: number
  photoCount: number
}
