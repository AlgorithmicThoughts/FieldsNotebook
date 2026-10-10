import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { Icon } from '@/components/common/Icon'

interface ReportEmptyProps {
  icon: LucideIcon
  title: string
  detail: string
  /** Control the state offers, such as a retry after a failed request. */
  action?: ReactNode
}

/** One empty state. The page uses a different message for each reason. */
export function ReportEmpty({ icon, title, detail, action }: ReportEmptyProps) {
  return (
    <div className="report-empty">
      <Icon icon={icon} size={30} />
      <strong>{title}</strong>
      <p>{detail}</p>
      {action ? <div className="report-empty-action">{action}</div> : null}
    </div>
  )
}
