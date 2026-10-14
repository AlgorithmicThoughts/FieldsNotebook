import type { DelayReportRow } from '@/domain/reports'
import type { ApiQueryState } from '@/hooks/useApiQuery'

/**
 * DelayStatus.Name for a resolved delay. A revoked delay also carries a
 * ClosedAt on its final cycle, so the closure timestamp alone cannot tell the
 * two apart.
 */
const RESOLVED_STATUS_NAME = 'Resolved'

/** One reason tag and how often the returned scope used it, zero included. */
export interface ReportTagCount {
  /** DelayTags.Name, from the lookup rather than from the rows. */
  name: string
  count: number
}

/** Honest totals derived from the rows on screen. No estimate, no forecast. */
export interface ReportSummary {
  total: number
  resolved: number
  tagCounts: ReportTagCount[]
}

/**
 * Every approved reason starts at zero, so the breakdown lists all nine tags
 * whatever the scope returned. A Map keeps the lookup's insertion order, and a
 * row carrying a tag outside the lookup still lands at the end rather than
 * disappearing.
 */
function countByTag(rows: DelayReportRow[], tagNames: string[]): Map<string, number> {
  const counts = new Map<string, number>()

  for (const name of tagNames) {
    counts.set(name, 0)
  }

  for (const row of rows) {
    counts.set(row.tagName, (counts.get(row.tagName) ?? 0) + 1)
  }

  return counts
}

/**
 * Busiest reason first. Array sort is stable, so the eight-way tie among the
 * zeros keeps the DelayTags lookup order instead of reshuffling alphabetically
 * every time a count changes.
 */
function byCount(left: ReportTagCount, right: ReportTagCount): number {
  return right.count - left.count
}

/**
 * What the preview column shows. An empty `ready` result is not the same as
 * `no-scope`: one says the scope returned nothing, the other asks for a project.
 */
export type PreviewView =
  | { kind: 'no-scope' }
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; rows: DelayReportRow[]; tagNames: string[] }

/** The report result the page may render; `idle` until a project is in scope. */
export type ReportState = ApiQueryState<DelayReportRow[]>

/**
 * The approved reason tags. They never change with the scope, so the page
 * loads them once rather than with every report request.
 */
export type TagState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; tagNames: string[] }

type ReportView = Exclude<PreviewView, { kind: 'ready' }> | { kind: 'ready'; rows: DelayReportRow[] }

function toReportView(state: ReportState): ReportView {
  switch (state.status) {
    case 'idle':
      return { kind: 'no-scope' }
    case 'loading':
      return { kind: 'loading' }
    case 'error':
      return { kind: 'error', message: state.message }
    case 'ready':
      return { kind: 'ready', rows: state.data }
  }
}

/**
 * The breakdown lists every approved tag, so rows render only once the tags
 * are in hand too; a partial breakdown never reaches the screen.
 */
export function toPreviewView(state: ReportState, tags: TagState): PreviewView {
  const report = toReportView(state)

  if (report.kind !== 'ready') {
    return report
  }

  if (tags.status !== 'ready') {
    return tags.status === 'error' ? { kind: 'error', message: tags.message } : { kind: 'loading' }
  }

  return { kind: 'ready', rows: report.rows, tagNames: tags.tagNames }
}

export function toReportSummary(rows: DelayReportRow[], tagNames: string[]): ReportSummary {
  return {
    total: rows.length,
    resolved: rows.filter((row) => row.statusName === RESOLVED_STATUS_NAME).length,
    tagCounts: [...countByTag(rows, tagNames)]
      .map(([name, count]) => ({ name, count }))
      .sort(byCount),
  }
}
