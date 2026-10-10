import { BellRing, CircleCheck, TriangleAlert } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Card, CardHeading } from '@/components/common/Card'
import { Icon } from '@/components/common/Icon'
import { EmptyDelays } from '@/components/delays/EmptyDelays'
import type { NotificationSummary } from '@/domain/notifications'
import { useWorkspace } from '@/routes/useWorkspace'
import { useAppState } from '@/state/appState'
import type { NoticesView } from '@/state/useUnreadNotices'
import { NoticeRow } from './components/NoticeRow'
import { NoticesSkeleton } from './components/NoticesSkeleton'
import { noticeChannelsOf, type NoticeChannel } from './notificationsView'
import './NotificationsPage.css'

interface NoticesBodyProps {
  notices: NoticesView
  channels: NoticeChannel[]
  onRetry: () => void
  onOpenDelay: (notice: NotificationSummary) => void
}

function NoticesBody({ notices, channels, onRetry, onOpenDelay }: NoticesBodyProps) {
  if (notices.status === 'loading') {
    return <NoticesSkeleton />
  }

  if (notices.status === 'error') {
    return (
      <div className="notices-message" role="alert">
        <h3>Your notices are not available</h3>
        <p className="muted small">{notices.message}</p>
        <Button label="Try again" variant="primary" onClick={onRetry} />
      </div>
    )
  }

  if (notices.notices.length === 0) {
    return (
      <EmptyDelays
        title="You are up to date"
        detail="There are no notices for you in this project."
      />
    )
  }

  return (
    <>
      {notices.notices.map((notice) => (
        <NoticeRow
          key={notice.id}
          notice={notice}
          channels={channels}
          onOpenDelay={onOpenDelay}
        />
      ))}
    </>
  )
}

/**
 * The signed-in user's notices for the active project. Opening this page marks
 * nothing read, there is no bulk read control, and a read grants no workflow
 * permission of any kind.
 */
export function NotificationsPage() {
  const { user, project } = useWorkspace()
  const { notices, unreadNoticeCount, noticeReadError, reloadNotices, markNoticeRead } =
    useAppState()
  const navigate = useNavigate()
  const channels = noticeChannelsOf(user)

  /** The read is recorded first, then the delay opens whether or not it saved. */
  function openDelay(notice: NotificationSummary) {
    markNoticeRead(notice.id)
    navigate(`/tracking?delay=${notice.delayNumber}`)
  }

  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{project.name}</div>
          <h1>Notifications</h1>
          <p>
            Tagged people receive delay notices. Superintendents receive handover answers.
          </p>
        </div>
      </div>

      <Card>
        <CardHeading
          title="Your notices"
          action={
            notices.status === 'ready' ? (
              <Badge
                label={unreadNoticeCount === 0 ? 'All read' : `${unreadNoticeCount} unread`}
                tone={unreadNoticeCount === 0 ? 'resolved' : 'urgent'}
                icon={unreadNoticeCount === 0 ? CircleCheck : BellRing}
              />
            ) : null
          }
        />

        {noticeReadError ? (
          <p className="notice-error" role="alert">
            <Icon icon={TriangleAlert} size={16} />
            {noticeReadError} The notice stays unread.
          </p>
        ) : null}

        <NoticesBody
          notices={notices}
          channels={channels}
          onRetry={reloadNotices}
          onOpenDelay={openDelay}
        />
      </Card>

      <p className="notices-note">
        A notice is read only when you open its delay. Field Notebook sends notices in the app,
        and by email or SMS when you turn those channels on in Settings.
      </p>
    </>
  )
}
