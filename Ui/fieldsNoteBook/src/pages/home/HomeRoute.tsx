import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getRecentActivityByProjectId } from '@/api/activity'
import { getDelaysByProjectId } from '@/api/delays'
import { isAbortError, isApiError } from '@/api/errors'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { AddBlockerModal } from '@/components/delays/AddBlockerModal'
import type { ActivityEntry } from '@/domain/activity'
import type { DelaySummary } from '@/domain/delays'
import { canReportDelay } from '@/domain/permissions'
import { formatProjectDate, formatProjectTime } from '@/lib/projectTime'
import { useWorkspace } from '@/routes/useWorkspace'
import { HomePage } from './HomePage'
import { HomeSkeleton } from './components/HomeSkeleton'
import {
  ACTIVITY_LIMIT,
  toAdvanceNotice,
  toAttentionDelays,
  toHomeMetrics,
} from './homeView'

interface HomeData {
  delays: DelaySummary[]
  activity: ActivityEntry[]
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

  /** A delay the Foreman just raised joins the list without another request. */
  function addDelay(delay: DelaySummary) {
    setState((current) =>
      current.key !== null && current.status === 'ready'
        ? { ...current, data: { ...current.data, delays: [delay, ...current.data.delays] } }
        : current,
    )
  }

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    Promise.all([
      getDelaysByProjectId(projectId, { signal }),
      getRecentActivityByProjectId(projectId, ACTIVITY_LIMIT, { signal }),
    ])
      .then(([delays, activity]) => {
        if (signal.aborted) {
          return
        }

        setState({
          key: requestKey,
          status: 'ready',
          data: { delays, activity, fetchedAt: new Date() },
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

  const { delays, activity, fetchedAt } = state.data
  const projectName = user.projects.find((project) => project.id === projectId)?.name ?? ''

  return (
    <>
      <HomePage
        projectName={projectName}
        crewName={user.crewName}
        roleName={user.roleName}
        todayLabel={formatProjectDate(fetchedAt)}
        canReportDelay={canReportDelay(user)}
        advanceNotice={toAdvanceNotice(delays, user)}
        metrics={toHomeMetrics(delays, user)}
        attentionDelays={toAttentionDelays(delays, user)}
        activityEntries={activity}
        lastCheckedLabel={formatProjectTime(fetchedAt)}
        onReportDelay={() => setIsReportOpen(true)}
        onOpenDelay={(delayNumber) => navigate(`/tracking?delay=${delayNumber}`)}
        onViewAllDelays={() => navigate('/tracking')}
      />

      <AddBlockerModal
        open={isReportOpen}
        onOpenChange={setIsReportOpen}
        user={user}
        projectId={projectId}
        onCreated={addDelay}
      />
    </>
  )
}
