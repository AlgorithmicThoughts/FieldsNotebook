import { ShieldCheck } from 'lucide-react'
import { Icon } from '@/components/common/Icon'

interface HandoverTipProps {
  title: string
  detail: string
}

export function HandoverTip({ title, detail }: HandoverTipProps) {
  return (
    <div className="tip">
      <Icon icon={ShieldCheck} />
      <div>
        <strong>{title}</strong>
        <p>{detail}</p>
      </div>
    </div>
  )
}
