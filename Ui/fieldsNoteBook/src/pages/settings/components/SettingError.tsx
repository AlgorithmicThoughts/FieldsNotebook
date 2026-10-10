import { TriangleAlert } from 'lucide-react'
import { Icon } from '@/components/common/Icon'

interface SettingErrorProps {
  message: string | null
}

/** A failed save, so the card never shows a preference it did not store. */
export function SettingError({ message }: SettingErrorProps) {
  if (message === null) {
    return null
  }

  return (
    <p className="setting-error" role="alert">
      <Icon icon={TriangleAlert} size={16} />
      {message}
    </p>
  )
}
