import { ArrowUpRight } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Card } from '@/components/common/Card'
import { Icon } from '@/components/common/Icon'
import type { SiteDocument, SiteDocumentId } from '../documentsData'

interface DocumentCardProps {
  item: SiteDocument
  onView: (id: SiteDocumentId) => void
}

/** One sample document, opening its file or its preview. There is no upload. */
export function DocumentCard({ item, onView }: DocumentCardProps) {
  return (
    <Card className="h-full">
      <div className="grid h-full content-start justify-items-start p-[22px]">
        <div className="grid size-[52px] place-items-center rounded-full bg-[var(--soft)] text-[var(--accent)]">
          <Icon icon={item.icon} />
        </div>

        <div className="eyebrow mt-5">{item.category}</div>
        <h2 className="mt-[7px]">{item.title}</h2>
        <p className="small muted mt-2">{item.note}</p>

        <div className="mt-[22px] flex w-full flex-wrap items-center justify-between gap-[10px]">
          <Badge label={item.version} tone="outline" />
          {item.fileUrl ? (
            <Button
              label="View"
              icon={ArrowUpRight}
              nativeButton={false}
              render={<a href={item.fileUrl} target="_blank" rel="noreferrer" />}
            />
          ) : (
            <Button label="View" icon={ArrowUpRight} onClick={() => onView(item.id)} />
          )}
        </div>
      </div>
    </Card>
  )
}
