import { RefreshCw } from 'lucide-react'
import { Icon } from '@/components/common/Icon'

interface RefreshNoteProps {
  /** Time of the last delay refresh request. */
  lastCheckedLabel: string
}

export function RefreshNote({ lastCheckedLabel }: RefreshNoteProps) {
  return (
    <div className="refresh-note">
      <Icon icon={RefreshCw} size={13} />
      Last check {lastCheckedLabel} · Home and foreground refresh
    </div>
  )
}
