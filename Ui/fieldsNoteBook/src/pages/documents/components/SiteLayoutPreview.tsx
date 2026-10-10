import { SiteMap } from '@/components/delays/SiteMap'
import { Skeleton } from '@/components/ui/skeleton'
import { useProjectSiteMap } from '../useProjectSiteMap'

interface SiteLayoutPreviewProps {
  projectId: number
}

/**
 * The project site image, read-only and without pins. This is a reference
 * view of the layout, not the site map page, so nothing here places a pin.
 */
export function SiteLayoutPreview({ projectId }: SiteLayoutPreviewProps) {
  const state = useProjectSiteMap(projectId)

  if (state.status === 'loading') {
    return <Skeleton className="h-[220px] w-full rounded-[12px]" aria-busy="true" />
  }

  if (state.status === 'error') {
    return (
      <p className="small muted" role="alert">
        {state.message}
      </p>
    )
  }

  if (state.siteMap === null) {
    return <p className="small muted">This project has no site image.</p>
  }

  return (
    <>
      <SiteMap imageUrl={state.siteMap.uri} imageName={state.siteMap.name} pins={[]} compact />

      <p className="small muted mt-4">
        Sample layout of {state.siteMap.name}. The image has no GPS calibration, so it shows no
        measured position.
      </p>
    </>
  )
}
