import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getProjectHome, type ProjectHome } from '@/api/home'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { AddDelayModal } from '@/components/delays/AddDelayModal'
import type { DelaySummary } from '@/domain/delays'
import { canReportDelay } from '@/domain/permissions'
import { crewLabel } from '@/domain/users'
import { useApiQuery } from '@/hooks/useApiQuery'
import { formatProjectDate, formatProjectTime } from '@/lib/projectTime'
import { useWorkspace } from '@/routes/useWorkspace'
import { HomePage } from './HomePage'
import { HomeSkeleton } from './components/HomeSkeleton'
import { toActivityBadgeLabel, toAdvanceNotice, toHomeMetrics } from './homeView'

interface HomeData {
  home: ProjectHome
  fetchedAt: Date
}

const GENERIC_ERROR = 'Field Notebook could not load the delays for this project.'

async function loadHome(projectId: number, signal: AbortSignal): Promise<HomeData> {
  const home = await getProjectHome(projectId, { signal })

  return { home, fetchedAt: new Date() }
}

/**
 * Loads Home's data. Entering the route refreshes the delays, as the business
 * rules require; there is no scheduled poll.
 */
export function HomeRoute() {
  const { user } = useWorkspace()
  const navigate = useNavigate()
  const projectId = user.activeProjectId
  const [isReportOpen, setIsReportOpen] = useState(false)
  const { state, reload, update } = useApiQuery(
    `home:${projectId}`,
    (signal) => loadHome(projectId, signal),
    GENERIC_ERROR,
  )

  /**
   * A delay the Foreman just raised joins the list without another request.
   * The counts refresh on the next visit, when the API includes it.
   */
  function addDelay(delay: DelaySummary) {
    update((data) => ({
      ...data,
      home: { ...data.home, attentionDelays: [delay, ...data.home.attentionDelays] },
    }))
  }

  if (state.status === 'idle' || state.status === 'loading') {
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
            onClick={reload}
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
        activityBadgeLabel={toActivityBadgeLabel(home.recentActivity, fetchedAt)}
        lastCheckedLabel={formatProjectTime(fetchedAt)}
        onReportDelay={() => setIsReportOpen(true)}
        onOpenDelay={(delayNumber) => void navigate(`/tracking?delay=${delayNumber}`)}
        onViewAllDelays={() => void navigate('/tracking')}
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
