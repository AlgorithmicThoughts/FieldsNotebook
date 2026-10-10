import { Badge } from '@/components/common/Badge'
import { Modal } from '@/components/common/Modal'
import type { SiteDocument } from '../documentsData'
import { SafetyPreview } from './SafetyPreview'
import { SchedulePreview } from './SchedulePreview'
import { SiteLayoutPreview } from './SiteLayoutPreview'

interface PreviewBodyProps {
  item: SiteDocument
  projectId: number
}

function PreviewBody({ item, projectId }: PreviewBodyProps) {
  if (item.id === 'safety') {
    return <SafetyPreview />
  }

  if (item.id === 'schedule') {
    return <SchedulePreview />
  }

  return <SiteLayoutPreview projectId={projectId} />
}

interface DocumentPreviewModalProps {
  item: SiteDocument | null
  projectId: number
  projectName: string
  onClose: () => void
}

/** A sample preview of one document. There is no viewer, download, or upload. */
export function DocumentPreviewModal({
  item,
  projectId,
  projectName,
  onClose,
}: DocumentPreviewModalProps) {
  if (item === null) {
    return null
  }

  return (
    <Modal open title={item.title} onOpenChange={onClose}>
      <div className="row wrap mb-4">
        <Badge label={projectName} tone="outline" />
        <Badge label={item.version} tone="outline" />
      </div>

      <PreviewBody item={item} projectId={projectId} />
    </Modal>
  )
}
