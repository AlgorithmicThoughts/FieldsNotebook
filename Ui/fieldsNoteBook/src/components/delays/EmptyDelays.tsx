import { CircleCheck } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import './delays.css'

interface EmptyDelaysProps {
  title: string
  detail: string
}

export function EmptyDelays({ title, detail }: EmptyDelaysProps) {
  return (
    <div className="empty">
      <Icon icon={CircleCheck} size={30} />
      <strong>{title}</strong>
      <p className="small">{detail}</p>
    </div>
  )
}
