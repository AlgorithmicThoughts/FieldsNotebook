import { CalendarDays, FileText, ShieldCheck, type LucideIcon } from 'lucide-react'

/** The sample preview a document opens. The id also selects the preview body. */
export type SiteDocumentId = 'safety' | 'schedule' | 'siteLayout'

export interface SiteDocument {
  id: SiteDocumentId
  /** Eyebrow text above the title. */
  category: string
  title: string
  /** One-line description. */
  note: string
  /** Shown as an outline Badge. */
  version: string
  icon: LucideIcon
  /** A sample file View opens in a new tab instead of the preview modal. */
  fileUrl?: string
}

// Sample documents. No table backs this content. The approved design presents
// these as demonstrations of the document experience, not as site instructions.
export const SITE_DOCUMENTS: SiteDocument[] = [
  {
    id: 'safety',
    category: 'Safety',
    title: 'Site safety plan',
    note: 'Project safety reference',
    version: 'Sample · Rev 03',
    icon: ShieldCheck,
    // A placeholder PDF that says Hello World. It holds no safety instructions.
    fileUrl: `${import.meta.env.BASE_URL}sample-safety-plan.pdf`,
  },
  {
    id: 'schedule',
    category: 'Schedule',
    title: 'Work schedule',
    note: 'Planned crew handovers',
    version: 'Sample · 8 October',
    icon: CalendarDays,
  },
  {
    id: 'siteLayout',
    category: 'Plans',
    title: 'Fabrication area plan',
    note: 'Bay and access layout',
    version: 'Sample · Rev 02',
    icon: FileText,
  },
]
