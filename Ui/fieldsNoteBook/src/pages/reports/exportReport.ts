import type { DelayReportRow } from '@/domain/reports'

/** Two spaces, so a planner can read the file without a formatter. */
const JSON_INDENT = 2

/**
 * Downloads the rows the preview is showing. Timestamps stay ISO 8601 UTC in
 * the file; the screen is the only place that formats them.
 */
export function downloadReportJson(rows: DelayReportRow[], fileName: string): void {
  const blob = new Blob([JSON.stringify(rows, null, JSON_INDENT)], {
    type: 'application/json',
  })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}
