/**
 * One project as the shell shows it. The table has no place or address column,
 * so the district stands in for the location line.
 */
export interface ProjectSummary {
  id: number
  name: string
  /** Projects.Code, such as OMA-FAB. */
  code: string
  /** Districts.Name, or null when the district row is missing. */
  districtName: string | null
}
