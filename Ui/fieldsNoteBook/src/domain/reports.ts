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

/**
 * One delay, flattened with every identifier resolved to a name so an exported
 * file reads without the database behind it.
 */
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

/** Stands in for an open bound so the file name still reads as a range. */
const OPEN_BOUND = { from: 'earliest', to: 'latest' }

/**
 * Names the export after its scope: the project codes and the requested range.
 * Timestamps inside the file stay ISO 8601 UTC.
 */
export function buildReportFileName(
  projectCodes: string[],
  dateFrom?: string,
  dateTo?: string,
): string {
  const scope = projectCodes.join('-')
  const from = dateFrom ?? OPEN_BOUND.from
  const to = dateTo ?? OPEN_BOUND.to

  return `field-notebook-delays_${scope}_${from}_to_${to}.json`
}
