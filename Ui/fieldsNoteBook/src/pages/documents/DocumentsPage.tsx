import { useState } from 'react'
import { Info } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import { useWorkspace } from '@/routes/useWorkspace'
import { DocumentCard } from './components/DocumentCard'
import { DocumentPreviewModal } from './components/DocumentPreviewModal'
import { SITE_DOCUMENTS, type SiteDocumentId } from './documentsData'
import './DocumentsPage.css'

/** Site reference documents for the active project, each opening a sample preview. */
export function DocumentsPage() {
  const { user, project } = useWorkspace()
  const [openDocumentId, setOpenDocumentId] = useState<SiteDocumentId | null>(null)
  const openDocument = SITE_DOCUMENTS.find((item) => item.id === openDocumentId) ?? null

  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{project.name}</div>
          <h1>Site documents</h1>
          <p>Find the current site reference before you start work.</p>
        </div>
      </div>

      <div className="document-notice">
        <Icon icon={Info} />
        <div>
          <strong>Sample documents</strong>
          <p>
            These files demonstrate the document experience. They contain no approved safety
            instructions or construction plans.
          </p>
        </div>
      </div>

      <div className="document-grid">
        {SITE_DOCUMENTS.map((item) => (
          <DocumentCard key={item.id} item={item} onView={setOpenDocumentId} />
        ))}
      </div>

      <p className="document-hint">
        Documents belong to the selected project. Switch projects to view another site's documents.
      </p>

      <DocumentPreviewModal
        item={openDocument}
        projectId={user.activeProjectId}
        projectName={project.name}
        onClose={() => setOpenDocumentId(null)}
      />
    </>
  )
}
