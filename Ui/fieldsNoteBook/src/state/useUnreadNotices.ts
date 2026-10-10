import { useEffect, useState } from 'react'
import { isAbortError, isApiError } from '@/api/errors'
import { getProjectNotices, markNotificationRead } from '@/api/notifications'
import type { NotificationSummary } from '@/domain/notifications'
import type { CurrentUser } from '@/domain/users'

const LOAD_FAILED = 'Field Notebook could not load your notices for this project.'
const READ_FAILED = 'Field Notebook could not record that you read this notice.'

/** What the notifications page renders. */
export type NoticesView =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; notices: NotificationSummary[] }

/** `key` records the request the result belongs to, so a late answer for a
 * previous project or attempt is ignored rather than rendered. */
type LoadedNotices =
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; notices: NotificationSummary[]; unreadCount: number }

export interface UnreadNoticesState {
  notices: NoticesView
  /**
   * Unread total for the active project from the API, shown by the shell's bell,
   * sidebar badge, and notifications page. Zero while it loads or when it
   * fails, so no stale badge.
   */
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
 * The signed-in user's notices for the active project. One request supplies
 * both the list and the unread total, and a read returns the new total, so the
 * shell never polls a separate count.
 */
export function useUnreadNotices(user: CurrentUser): UnreadNoticesState {
  const [loaded, setLoaded] = useState<LoadedNotices | null>(null)
  const [reloadCount, setReloadCount] = useState(0)
  const [noticeReadError, setNoticeReadError] = useState<string | null>(null)
  const projectId = user.activeProjectId
  const projectName = user.projects.find((project) => project.id === projectId)?.name ?? ''
  const key = `${user.id}:${projectId}:${reloadCount}`

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    getProjectNotices(projectId, projectName, { signal })
      .then(({ notices, unreadCount }) => {
        if (!signal.aborted) {
          setLoaded({ key, status: 'ready', notices, unreadCount })
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
  }, [projectId, projectName, key])

  const current = loaded !== null && loaded.key === key ? loaded : null
  const notices: NoticesView = current ?? { status: 'loading' }
  const unreadNoticeCount = current?.status === 'ready' ? current.unreadCount : 0

  function markNoticeRead(notificationId: number) {
    // An opened notice keeps its first read time, so there is nothing to send.
    if (current?.status === 'ready' && current.notices.some((n) => n.id === notificationId && n.isRead)) {
      return
    }

    const requestKey = key
    setNoticeReadError(null)

    markNotificationRead(projectId, projectName, notificationId)
      .then(({ notice: read, unreadCount }) => {
        // A read that answers after a project switch or reload must not touch the newer list.
        setLoaded((latest) =>
          latest === null || latest.key !== requestKey || latest.status === 'error'
            ? latest
            : {
                ...latest,
                unreadCount,
                notices: latest.notices.map((notice) => (notice.id === read.id ? read : notice)),
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
