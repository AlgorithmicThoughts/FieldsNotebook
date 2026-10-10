import { projectHomePath, type ProjectHome } from './api/home'
import type { CurrentUser, CurrentUserProject } from './api/currentUser'
import { unreadNotificationCountPath, type UnreadNotificationCount } from './api/notifications'
import { AppShell } from './components/layout/AppShell'
import { buildNavItems } from './components/layout/navItems'
import { Sidebar } from './components/layout/Sidebar'
import { TopBar } from './components/layout/TopBar'
import { formatLocalTimeZoneLabel, formatTimeLabel, formatTodayLabel } from './domain/time'
import { toCrewLabel, toInitials } from './domain/users'
import { useApiData } from './hooks/useApiData'
import type { ResolvedTheme } from './hooks/useResolvedTheme'
import { HomePage } from './pages/home/HomePage'
import { toActivityBadgeLabel, toActivityEntries, toAdvanceNotice, toHomeMetrics } from './pages/home/homeContent'

interface ProjectWorkspaceProps {
  user: CurrentUser
  project: CurrentUserProject
  /** When the current user loaded; anchors "today" and the time zone label. */
  loadedAt: Date
  theme: ResolvedTheme
  onThemeToggle: () => void
}

/** The desktop frame and Home page for one selected project. */
export function ProjectWorkspace({ user, project, loadedAt, theme, onThemeToggle }: ProjectWorkspaceProps) {
  // Requested together: the badge belongs to the shell on every screen, Home owns the rest.
  const home = useApiData<ProjectHome>(projectHomePath(project.projectId))
  const unread = useApiData<UnreadNotificationCount>(unreadNotificationCountPath(project.projectId))
  // A failed or pending count shows no badge rather than a guess.
  const unreadCount = unread.data?.unreadCount ?? 0
  const crewLabel = toCrewLabel(project)

  return (
    <AppShell
      header={
        <TopBar
          projectName={project.name}
          projectCode={project.code}
          projectPlace={project.districtName}
          userDisplayName={user.displayName}
          userInitials={toInitials(user.displayName)}
          userRoleName={project.roleName}
          userCrewName={crewLabel}
          theme={theme}
          hasUnreadNotifications={unreadCount > 0}
          onThemeToggle={onThemeToggle}
        />
      }
      sidebar={
        <Sidebar
          items={buildNavItems(unreadCount)}
          activeItemId="home"
          crewName={crewLabel}
          roleName={project.roleName}
          projectTimeLabel={formatLocalTimeZoneLabel(loadedAt)}
        />
      }
    >
      {home.data && home.loadedAt ? (
        <HomePage
          projectName={project.name}
          crewName={crewLabel}
          roleName={project.roleName}
          todayLabel={formatTodayLabel(loadedAt)}
          canReportDelay={project.canReportDelay}
          advanceNotice={home.data.advanceNotice ? toAdvanceNotice(home.data.advanceNotice, home.loadedAt) : null}
          metrics={toHomeMetrics(home.data.metrics)}
          attentionDelays={home.data.attentionDelays}
          activityEntries={toActivityEntries(home.data.recentActivity, home.loadedAt)}
          activityBadgeLabel={toActivityBadgeLabel(home.data.recentActivity, home.loadedAt)}
          lastCheckedLabel={formatTimeLabel(home.loadedAt)}
        />
      ) : (
        <p className="muted" role="status">
          {home.error ? `Delays could not load: ${home.error.message}` : 'Loading delays…'}
        </p>
      )}
    </AppShell>
  )
}
