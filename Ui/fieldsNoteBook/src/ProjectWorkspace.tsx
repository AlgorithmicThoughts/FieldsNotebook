import { projectHomePath, type ProjectHome } from './api/home'
import type { CurrentUser, CurrentUserProject } from './api/currentUser'
import { AppShell } from './components/layout/AppShell'
import { Sidebar } from './components/layout/Sidebar'
import { TopBar } from './components/layout/TopBar'
import { formatLocalTimeZoneLabel, formatTimeLabel, formatTodayLabel } from './domain/time'
import { toCrewLabel, toInitials } from './domain/users'
import { useApiData } from './hooks/useApiData'
import type { ResolvedTheme } from './hooks/useResolvedTheme'
import { HomePage } from './pages/home/HomePage'
import { toAdvanceNotice, toHomeMetrics } from './pages/home/homeContent'
import { ACTIVITY_ENTRIES, HAS_UNREAD_NOTIFICATIONS, NAV_ITEMS } from './pages/home/homeData'

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
  const home = useApiData<ProjectHome>(projectHomePath(project.projectId))
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
          hasUnreadNotifications={HAS_UNREAD_NOTIFICATIONS}
          onThemeToggle={onThemeToggle}
        />
      }
      sidebar={
        <Sidebar
          items={NAV_ITEMS}
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
          activityEntries={ACTIVITY_ENTRIES}
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
