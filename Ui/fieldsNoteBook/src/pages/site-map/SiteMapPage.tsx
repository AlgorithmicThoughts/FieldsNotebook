import { useState } from 'react'
import { getProjectTracking, type ProjectTracking } from '@/api/tracking'
import type { DelayPhotoPreview } from '@/api/siteMaps'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { Skeleton } from '@/components/ui/skeleton'
import type { DelaySummary, SiteMapOption } from '@/domain/delays'
import { useApiQuery } from '@/hooks/useApiQuery'
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

const GENERIC_ERROR = 'Field Notebook could not load the site map for this project.'

function latestPhotos(tracking: ProjectTracking): DelayPhotoPreview[] {
  return tracking.delays.flatMap((delay) => {
    const photo = delay.photos.flatMap((group) => group.photos)
      .sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt)).at(0)
    return photo === undefined ? [] : [{ delayId: delay.summary.id, ...photo }]
  })
}

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
  const [selectedDelayNumber, setSelectedDelayNumber] = useState<string | null>(null)
  const [isSummaryOpen, setIsSummaryOpen] = useState(false)
  const { state, reload } = useApiQuery(
    `site-map:${projectId}`,
    async (signal): Promise<SiteMapData> => {
      const tracking = await getProjectTracking(projectId, { signal })

      // Fresh delays may no longer hold the pin the user had open.
      setSelectedDelayNumber(null)
      setIsSummaryOpen(false)

      return {
        siteMap: tracking.delays.find((row) => row.siteMap !== null)?.siteMap ?? null,
        delays: tracking.delays.map((row) => row.summary),
        photos: latestPhotos(tracking),
      }
    },
    GENERIC_ERROR,
  )

  if (state.status === 'idle' || state.status === 'loading') {
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
            onClick={reload}
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
        onDelayChanged={reload}
      />
    </>
  )
}
