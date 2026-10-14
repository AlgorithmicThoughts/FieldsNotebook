import { useState } from 'react'
import { getDelayReport } from '@/api/reports'
import type { DelayReportRequest } from '@/domain/reports'
import { useApiQuery } from '@/hooks/useApiQuery'
import { useWorkspace } from '@/routes/useWorkspace'
import { ReportPreview } from './components/ReportPreview'
import { ReportScopeCard } from './components/ReportScopeCard'
import { toPreviewView } from './reportsView'
import { useDebouncedValue } from './useDebouncedValue'
import { useDelayTags } from './useDelayTags'
import './ReportsPage.css'

/** Long enough that typing a year fires one request, short enough to feel live. */
const DATE_DEBOUNCE_MS = 400

const GENERIC_ERROR = 'Field Notebook could not build this delay report.'

/** The two date inputs travel together, so one settled string debounces both. */
function toRequest(projectIds: number[], settledDates: string): DelayReportRequest {
  const [dateFrom, dateTo] = settledDates.split('|')

  return {
    projectIds,
    dateFrom: dateFrom === '' ? undefined : dateFrom,
    dateTo: dateTo === '' ? undefined : dateTo,
  }
}

export function ReportsPage() {
  const { user, project } = useWorkspace()
  const [selectedProjectIds, setSelectedProjectIds] = useState<number[]>([user.activeProjectId])
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const { tags, retry: retryTags } = useDelayTags()

  const settledDates = useDebouncedValue(`${dateFrom}|${dateTo}`, DATE_DEBOUNCE_MS)
  const request = toRequest(selectedProjectIds, settledDates)
  const hasScope = selectedProjectIds.length > 0
  // No project in scope means nothing to ask for, so the query stays idle.
  const { state, reload } = useApiQuery(
    hasScope ? `report:${selectedProjectIds.join(',')}|${settledDates}` : null,
    (signal) => getDelayReport(request, { signal }),
    GENERIC_ERROR,
  )

  const view = toPreviewView(state, tags)

  /** Retries whichever request failed; a loaded tag list is kept. */
  function retry() {
    retryTags()

    if (state.status === 'error') {
      reload()
    }
  }

  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{project.name}</div>
          <h1>Delay reports</h1>
          <p>Turn field records into better plans for the next project.</p>
        </div>
      </div>

      <div className="report-grid">
        <ReportScopeCard
          projects={user.projects}
          selectedProjectIds={selectedProjectIds}
          onSelectedProjectIdsChange={setSelectedProjectIds}
          dateFrom={dateFrom}
          dateTo={dateTo}
          onDateFromChange={setDateFrom}
          onDateToChange={setDateTo}
        />

        <ReportPreview view={view} onRetry={retry} />
      </div>
    </>
  )
}
