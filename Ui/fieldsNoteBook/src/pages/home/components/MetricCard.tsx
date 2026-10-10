import type { LucideIcon } from 'lucide-react'
import { Icon } from '@/components/common/Icon'

export interface HomeMetric {
  id: string
  icon: LucideIcon
  value: number
  label: string
}

interface MetricCardProps {
  metric: HomeMetric
  onSelect?: (metricId: string) => void
}

/** A delay count for the current crew and project. */
export function MetricCard({ metric, onSelect }: MetricCardProps) {
  return (
    <button type="button" className="metric" onClick={() => onSelect?.(metric.id)}>
      <span className="metric-icon">
        <Icon icon={metric.icon} />
      </span>
      <span>
        <strong>{metric.value}</strong>
        <span className="metric-label">{metric.label}</span>
      </span>
    </button>
  )
}
