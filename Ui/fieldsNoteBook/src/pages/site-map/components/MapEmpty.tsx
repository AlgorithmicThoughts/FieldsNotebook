import type { LucideIcon } from 'lucide-react'
import { Icon } from '@/components/common/Icon'

interface MapEmptyProps {
  icon: LucideIcon
  title: string
  detail: string
}

export function MapEmpty({ icon, title, detail }: MapEmptyProps) {
  return (
    <div className="map-empty">
      <Icon icon={icon} size={30} />
      <strong>{title}</strong>
      <p className="small">{detail}</p>
    </div>
  )
}
