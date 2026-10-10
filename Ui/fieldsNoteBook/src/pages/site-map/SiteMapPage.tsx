import { useEffect, useState } from 'react'
import { getDelaysByProjectId } from '@/api/delays'
import { isAbortError, isApiError } from '@/api/errors'
import { getLatestDelayPhotosByProjectId, getSiteMapByProjectId } from '@/api/siteMaps'
import type { DelayPhotoPreview } from '@/api/siteMaps'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { Skeleton } from '@/components/ui/skeleton'
import type { DelaySummary, SiteMapOption } from '@/domain/delays'
import { useWorkspace } from '@/routes/useWorkspace'
import { DelayDetailModal } from '@/pages/tracking/components/DelayDetailModal'
import { SelectedDelayCard } from './components/SelectedDelayCard'
import { SiteMapCard } from './components/SiteMapCard'
import { toSiteMapPins } from './siteMapView'
import './SiteMapPage.css'

interface SiteMapData {
  siteMap: SiteMapOption | null
  delays: DelaySummary[]
  photos: DelayPhotoPreview[]
}

/**
 * `key` records the request the result belongs to, so a late answer for a
 * previous project or a previous attempt never renders.
 */
type SiteMapState =
  | { key: null }
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; data: SiteMapData }

const GENERIC_ERROR = 'Field Notebook could not load the site map for this project.'

function SiteMapSkeleton() {
  return (
    <div className="map-layout" aria-busy="true">
      <Skeleton className="h-[420px] w-full rounded-[var(--radius-card)]" />
      <Skeleton className="h-[260px] w-full rounded-[var(--radius-card)]" />
    </div>
  )
}

/** The active project's site image, its pins, and the delays without one. */
export function SiteMapPage() {
  const { user, project } = useWorkspace()
  const projectId = user.activeProjectId
  const [state, setState] = useState<SiteMapState>({ key: null })
  const [reloadCount, setReloadCount] = useState(0)
  const [selectedDelayNumber, setSelectedDelayNumber] = useState<string | null>(null)
  const [isSummaryOpen, setIsSummaryOpen] = useState(false)
  const requestKey = `${projectId}:${reloadCount}`

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    Promise.all([
      getSiteMapByProjectId(projectId, { signal }),
      getDelaysByProjectId(projectId, { signal }),
      getLatestDelayPhotosByProjectId(projectId, { signal }),
    ])
      .then(([siteMap, delays, photos]) => {
        if (signal.aborted) {
          return
        }

        setSelectedDelayNumber(null)
        setIsSummaryOpen(false)
        setState({ key: requestKey, status: 'ready', data: { siteMap, delays, photos } })
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
    return <SiteMapSkeleton />
  }

  if (state.status === 'error') {
    return (
      <Card>
        <div className="grid justify-items-start gap-3 p-[22px]" role="alert">
          <h2>The site map is not available</h2>
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

  const { siteMap, delays, photos } = state.data
  const pins = toSiteMapPins(delays)
  // The card beside the map always shows a pin, falling back to the first one.
  const pinned = delays.filter((delay) => delay.pin !== null)
  const selectedDelay =
    pinned.find((delay) => delay.number === selectedDelayNumber) ?? pinned[0] ?? null
  const selectedPhoto = photos.find((photo) => photo.delayId === selectedDelay?.id) ?? null

  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{project.name}</div>
          <h1>See the delay. Find the place.</h1>
          <p>Select a site pin to see the delay and its photo.</p>
        </div>
      </div>

      <div className="map-layout">
        <SiteMapCard
          siteMap={siteMap}
          pins={pins}
          selectedDelayNumber={selectedDelay?.number ?? null}
          onSelectPin={setSelectedDelayNumber}
        />

        <aside>
          <SelectedDelayCard
            delay={selectedDelay}
            photo={selectedPhoto}
            onOpenDelay={() => setIsSummaryOpen(true)}
          />
        </aside>
      </div>

      <DelayDetailModal
        summary={isSummaryOpen ? selectedDelay : null}
        user={user}
        onClose={() => setIsSummaryOpen(false)}
      />
    </>
  )
}
