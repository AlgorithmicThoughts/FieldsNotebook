import { useEffect, useRef, useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { getProjectById } from '@/api/projects'
import { isAbortError, isApiError } from '@/api/errors'
import { getUser } from '@/api/users'
import { Button } from '@/components/common/Button'
import { AppShell } from '@/components/layout/AppShell'
import { Sidebar } from '@/components/layout/Sidebar'
import { TopBar } from '@/components/layout/TopBar'
import { Skeleton } from '@/components/ui/skeleton'
import type { ProjectSummary } from '@/domain/projects'
import type { CurrentUser } from '@/domain/users'
import { useResolvedTheme, type ThemePreference } from '@/hooks/useResolvedTheme'
import { PROJECT_TIME_LABEL } from '@/lib/projectTime'
import { NAV_ITEMS } from './navItems'
import { CURRENT_USER_ID } from './session'
import type { WorkspaceContext } from './useWorkspace'
import './routes.css'

/** `attempt` records which load produced the result, so a retry supersedes it. */
type SessionState =
  | { attempt: null }
  | { attempt: number; status: 'error'; message: string }
  | { attempt: number; status: 'ready'; user: CurrentUser; project: ProjectSummary }

const UNREAD_NOTIFICATIONS = NAV_ITEMS.some((item) => item.id === 'notifications' && item.count)

function messageOf(error: unknown): string {
  return isApiError(error) ? error.message : 'Field Notebook could not load your workspace.'
}

/** The shell every page renders inside. It owns the user, project, and theme. */
export function AppLayout() {
  const [session, setSession] = useState<SessionState>({ attempt: null })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    const load = async () => {
      const user = await getUser(CURRENT_USER_ID, { signal: controller.signal })
      const project = await getProjectById(user.activeProjectId, { signal: controller.signal })

      return { user, project }
    }

    load()
      .then(({ user, project }) => {
        if (controller.signal.aborted) {
          return
        }

        setSession({ attempt, status: 'ready', user, project })
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || controller.signal.aborted) {
          return
        }

        setSession({ attempt, status: 'error', message: messageOf(error) })
      })

    return () => controller.abort()
  }, [attempt])

  if (session.attempt !== attempt) {
    return <WorkspaceSkeleton />
  }

  if (session.status === 'error') {
    return (
      <div className="workspace-message" role="alert">
        <h1>Field Notebook is not available</h1>
        <p>{session.message}</p>
        <Button label="Try again" variant="primary" onClick={() => setAttempt((n) => n + 1)} />
      </div>
    )
  }

  return <Workspace user={session.user} project={session.project} />
}

interface WorkspaceProps {
  user: CurrentUser
  project: ProjectSummary
}

/**
 * Mounted once the user is known, so the saved theme preference seeds the
 * state directly instead of arriving through an Effect.
 */
function Workspace({ user, project }: WorkspaceProps) {
  const [preference, setPreference] = useState<ThemePreference>(
    () => user.themePreference.toLowerCase() as ThemePreference,
  )
  const theme = useResolvedTheme(preference)
  const { pathname } = useLocation()
  const isFirstRender = useRef(true)

  // Keyboard users land in the new page rather than back in the sidebar.
  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false
      return
    }

    document.getElementById('main')?.focus()
  }, [pathname])

  const context: WorkspaceContext = { user, project }

  return (
    <AppShell
      header={
        <TopBar
          projectName={project.name}
          projectCode={project.code}
          projectPlace={project.districtName ?? 'District not recorded'}
          userDisplayName={user.displayName}
          userInitials={user.initials}
          userRoleName={user.roleName}
          userCrewName={user.crewName}
          theme={theme}
          hasUnreadNotifications={UNREAD_NOTIFICATIONS}
          onThemeToggle={() => setPreference(theme === 'dark' ? 'light' : 'dark')}
        />
      }
      sidebar={
        <Sidebar
          items={NAV_ITEMS}
          crewName={user.crewName}
          roleName={user.roleName}
          projectTimeLabel={PROJECT_TIME_LABEL}
        />
      }
    >
      <Outlet context={context} />
    </AppShell>
  )
}

function WorkspaceSkeleton() {
  return (
    <div className="workspace-message" aria-busy="true">
      <Skeleton className="h-[86px] w-full" />
      <Skeleton className="h-8 w-72" />
      <Skeleton className="h-40 w-full" />
    </div>
  )
}
