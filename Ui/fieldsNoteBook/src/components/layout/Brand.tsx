import { ArrowRight } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import './layout.css'

/** The product lockup, shared by the top bar and the sign-in screen. */
export function Brand() {
  return (
    <div className="brand">
      <span className="brand-mark">
        <Icon icon={ArrowRight} size={23} />
      </span>
      Field Notebook
    </div>
  )
}
