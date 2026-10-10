import { useEffect, useMemo, useState } from 'react'
import { Download } from 'lucide-react'
import { isAbortError, isApiError } from '@/api/errors'
import { getDelayReport } from '@/api/reports'
import { Button } from '@/components/common/Button'
import { buildReportFileName, type DelayReportRequest } from '@/domain/reports'
import { useWorkspace } from '@/routes/useWorkspace'
import { ReportPreview } from './components/ReportPreview'
import { ReportScopeCard } from './components/ReportScopeCard'
import { downloadReportJson } from './exportReport'
import { toPreviewView, type ReportState } from './reportsView'
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
  const [reloadCount, setReloadCount] = useState(0)
  const [state, setState] = useState<ReportState>({ key: null, status: 'idle' })
  const { tags, retry: retryTags } = useDelayTags()

  const settledDates = useDebouncedValue(`${dateFrom}|${dateTo}`, DATE_DEBOUNCE_MS)
  const request = useMemo(
    () => toRequest(selectedProjectIds, settledDates),
    [selectedProjectIds, settledDates],
  )
  const hasScope = selectedProjectIds.length > 0
  const requestKey = `${selectedProjectIds.join(',')}|${settledDates}|${reloadCount}`

  useEffect(() => {
    if (!hasScope) {
      return
    }

    const controller = new AbortController()
    const { signal } = controller

    getDelayReport(request, { signal })
      .then((rows) => {
        if (!signal.aborted) {
          setState({ key: requestKey, status: 'ready', rows })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setState({
          key: requestKey,
          status: 'error',
          message: isApiError(error) ? error.message : GENERIC_ERROR,
        })
      })

    return () => controller.abort()
  }, [hasScope, request, requestKey])

  const view = toPreviewView(hasScope, state, requestKey, tags)
  const rows = view.kind === 'ready' ? view.rows : []

  /** Retries whichever request failed; a loaded tag list is kept. */
  function retry() {
    retryTags()

    if (state.status === 'error') {
      setReloadCount((count) => count + 1)
    }
  }

  function exportJson() {
    const codes = user.projects
      .filter((project) => selectedProjectIds.includes(project.id))
      .map((project) => project.code)

    downloadReportJson(rows, buildReportFileName(codes, request.dateFrom, request.dateTo))
  }

  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{project.name}</div>
          <h1>Delay reports</h1>
          <p>Turn field records into better plans for the next project.</p>
        </div>
        <Button
          label="Export JSON"
          variant="primary"
          icon={Download}
          disabled={rows.length === 0}
          onClick={exportJson}
        />
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
