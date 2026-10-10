import { ArrowRight, Bell, CircleDot } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'
import type { NotificationSummary } from '@/domain/notifications'
import { formatProjectTime } from '@/lib/projectTime'
import { formatRelativeTime, type NoticeChannel } from '../notificationsView'

interface NoticeRowProps {
  notice: NotificationSummary
  /** Channels the recipient's own preferences allow. */
  channels: NoticeChannel[]
  onOpenDelay: (notice: NotificationSummary) => void
}

/**
 * One notice. The row never marks itself read; only its own "Open delay" does,
 * and the read records nothing beyond the recipient having seen the notice.
 */
export function NoticeRow({ notice, channels, onOpenDelay }: NoticeRowProps) {
  return (
    <div className={notice.isRead ? 'notice-row' : 'notice-row unread'}>
      <span className="notice-symbol">
        <Icon icon={Bell} />
      </span>

      <div className="grow">
        <div className="row wrap notice-head">
          <strong>{notice.title}</strong>
          {notice.isRead ? null : <Badge label="Unread" tone="urgent" icon={CircleDot} />}
        </div>

        <p className="notice-detail">
          {notice.projectName} · {notice.delayNumber} · {notice.delayTitle}
        </p>

        <div className="row wrap notice-channels">
          {channels.map((channel) => (
            <Badge key={channel.id} label={channel.label} tone="outline" icon={channel.icon} />
          ))}
        </div>
      </div>

      <div className="notice-end">
        <span className="notice-time">
          {formatRelativeTime(notice.createdAt)}
          <small>
            {notice.isRead ? `Read ${formatProjectTime(notice.readAt ?? notice.createdAt)}` : 'Not read'}
          </small>
        </span>
        <Button
          label="Open delay"
          variant="quiet"
          icon={ArrowRight}
          iconPosition="end"
          onClick={() => onOpenDelay(notice)}
        />
      </div>
    </div>
  )
}
