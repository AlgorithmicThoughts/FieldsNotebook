import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getProjectHome, type ProjectHome } from '@/api/home'
import { isAbortError, isApiError } from '@/api/errors'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { AddDelayModal } from '@/components/delays/AddDelayModal'
import type { DelaySummary } from '@/domain/delays'
import { canReportDelay } from '@/domain/permissions'
import { crewLabel } from '@/domain/users'
import { formatProjectDate, formatProjectTime } from '@/lib/projectTime'
import { useWorkspace } from '@/routes/useWorkspace'
import { HomePage } from './HomePage'
import { HomeSkeleton } from './components/HomeSkeleton'
import { toAdvanceNotice, toHomeMetrics } from './homeView'

interface HomeData {
  home: ProjectHome
  fetchedAt: Date
}

/**
 * `key` records the request the result belongs to. A late reply for another
 * project, or for a previous attempt, no longer matches and never renders.
 */
type HomeState =
  | { key: null }
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; data: HomeData }

const GENERIC_ERROR = 'Field Notebook could not load the delays for this project.'

/**
 * Loads Home's data. Entering the route refreshes the delays, as the business
 * rules require; there is no scheduled poll.
 */
export function HomeRoute() {
  const { user } = useWorkspace()
  const navigate = useNavigate()
  const projectId = user.activeProjectId
  const [state, setState] = useState<HomeState>({ key: null })
  const [reloadCount, setReloadCount] = useState(0)
  const [isReportOpen, setIsReportOpen] = useState(false)
  const requestKey = `${projectId}:${reloadCount}`

  /**
   * A delay the Foreman just raised joins the list without another request.
   * The counts refresh on the next visit, when the API includes it.
   */
  function addDelay(delay: DelaySummary) {
    setState((current) =>
      current.key !== null && current.status === 'ready'
        ? {
            ...current,
            data: {
              ...current.data,
              home: { ...current.data.home, attentionDelays: [delay, ...current.data.home.attentionDelays] },
            },
          }
        : current,
    )
  }

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    getProjectHome(projectId, { signal })
      .then((home) => {
        if (signal.aborted) {
          return
        }

        setState({
          key: requestKey,
          status: 'ready',
          data: { home, fetchedAt: new Date() },
        })
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
  }, [projectId, requestKey])

  if (state.key !== requestKey) {
    return <HomeSkeleton />
  }

  if (state.status === 'error') {
    return (
      <Card>
        <div className="grid justify-items-start gap-3 p-[22px]" role="alert">
          <h2>Delays are not available</h2>
          <p className="muted">{state.message}</p>
          <Button
            label="Try again"
            variant="primary"
            onClick={() => setReloadCount((count) => count + 1)}
          />
        </div>
      </Card>
    )
  }

  const { home, fetchedAt } = state.data
  const projectName = user.projects.find((project) => project.id === projectId)?.name ?? ''

  return (
    <>
      <HomePage
        projectName={projectName}
        crewName={crewLabel(user)}
        roleName={user.roleName}
        todayLabel={formatProjectDate(fetchedAt)}
        canReportDelay={canReportDelay(user)}
        advanceNotice={toAdvanceNotice(home.advanceNotice, fetchedAt)}
        metrics={toHomeMetrics(home.metrics)}
        attentionDelays={home.attentionDelays}
        activityEntries={home.recentActivity}
        lastCheckedLabel={formatProjectTime(fetchedAt)}
        onReportDelay={() => setIsReportOpen(true)}
        onOpenDelay={(delayNumber) => navigate(`/tracking?delay=${delayNumber}`)}
        onViewAllDelays={() => navigate('/tracking')}
      />

      <AddDelayModal
        open={isReportOpen}
        onOpenChange={setIsReportOpen}
        user={user}
        projectId={projectId}
        onCreated={addDelay}
      />
    </>
  )
}
