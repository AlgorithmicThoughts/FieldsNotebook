import { useEffect, useState } from 'react'
import { isAbortError, isApiError } from '@/api/errors'
import { getNotificationsByUserId, markNotificationRead } from '@/api/notifications'
import type { NotificationSummary } from '@/domain/notifications'
import type { CurrentUser } from '@/domain/users'

const LOAD_FAILED = 'Field Notebook could not load your notices for this project.'
const READ_FAILED = 'Field Notebook could not record that you read this notice.'

/** What the notifications page renders. The count derives from the same list. */
export type NoticesView =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; notices: NotificationSummary[] }

/** `key` records the request the result belongs to, so a late answer for a
 * previous project or attempt is ignored rather than rendered. */
type LoadedNotices =
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; notices: NotificationSummary[] }

export interface UnreadNoticesState {
  notices: NoticesView
  /** Unread total for the active project, shown beside the sidebar item. */
  unreadNoticeCount: number
  /** Message from the last failed read. Opening the delay never depends on it. */
  noticeReadError: string | null
  reloadNotices: () => void
  /**
   * Records that the signed-in user opened this one notice. Nothing else marks
   * a notice read: not entering a page, not scrolling past it.
   */
  markNoticeRead: (notificationId: number) => void
}

/**
 * The signed-in user's notices for the active project. The shell reads the
 * unread total for the sidebar and the notifications page reads the list, so
 * both come from one copy of the data.
 */
export function useUnreadNotices(user: CurrentUser): UnreadNoticesState {
  const [loaded, setLoaded] = useState<LoadedNotices | null>(null)
  const [reloadCount, setReloadCount] = useState(0)
  const [noticeReadError, setNoticeReadError] = useState<string | null>(null)
  const userId = user.id
  const projectId = user.activeProjectId
  const key = `${userId}:${projectId}:${reloadCount}`

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    getNotificationsByUserId(userId, projectId, { signal })
      .then((notices) => {
        if (!signal.aborted) {
          setLoaded({ key, status: 'ready', notices })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setLoaded({
          key,
          status: 'error',
          message: isApiError(error) ? error.message : LOAD_FAILED,
        })
      })

    return () => controller.abort()
  }, [userId, projectId, key])

  const notices: NoticesView = loaded !== null && loaded.key === key ? loaded : { status: 'loading' }
  const unreadNoticeCount =
    notices.status === 'ready' ? notices.notices.filter((notice) => !notice.isRead).length : 0

  function markNoticeRead(notificationId: number) {
    setNoticeReadError(null)

    markNotificationRead(notificationId)
      .then((read) => {
        setLoaded((current) =>
          current === null || current.status === 'error'
            ? current
            : {
                ...current,
                notices: current.notices.map((notice) =>
                  notice.id === read.id ? read : notice,
                ),
              },
        )
      })
      .catch((error: unknown) => {
        // The notice simply stays unread. A failed read flag must never keep
        // the user from seeing the blocker.
        setNoticeReadError(isApiError(error) ? error.message : READ_FAILED)
      })
  }

  return {
    notices,
    unreadNoticeCount,
    noticeReadError,
    reloadNotices: () => setReloadCount((count) => count + 1),
    markNoticeRead,
  }
}
