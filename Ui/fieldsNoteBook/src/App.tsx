import { useState } from 'react'
import { CURRENT_USER_PATH, type CurrentUser } from './api/currentUser'
import { AppShell } from './components/layout/AppShell'
import { Sidebar } from './components/layout/Sidebar'
import { StartupStatus } from './components/layout/StartupStatus'
import { TopBar } from './components/layout/TopBar'
import { formatLocalTimeZoneLabel, formatTodayLabel } from './domain/time'
import { pickDefaultProject, toCrewLabel, toInitials } from './domain/users'
import { useApiData } from './hooks/useApiData'
import { useResolvedTheme, type ThemePreference } from './hooks/useResolvedTheme'
import { HomePage } from './pages/home/HomePage'
import {
  ACTIVITY_ENTRIES,
  ADVANCE_NOTICE,
  ATTENTION_DELAYS,
  HAS_UNREAD_NOTIFICATIONS,
  HOME_METRICS,
  LAST_CHECKED_LABEL,
  NAV_ITEMS,
} from './pages/home/homeData'

function App() {
  const [themePreference, setThemePreference] = useState<ThemePreference>('system')
  const theme = useResolvedTheme(themePreference)
  const currentUser = useApiData<CurrentUser>(CURRENT_USER_PATH)

  if (!currentUser.data || !currentUser.loadedAt) {
    return currentUser.error ? (
      <StartupStatus title="Field Notebook could not load your profile." detail={currentUser.error.message} />
    ) : (
      <StartupStatus title="Loading Field Notebook…" />
    )
  }

  const user = currentUser.data
  const project = pickDefaultProject(user.projects)
  if (!project) {
    return <StartupStatus title="No active project" detail={`${user.displayName} has no active project assignment.`} />
  }

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
          onThemeToggle={() => setThemePreference(theme === 'dark' ? 'light' : 'dark')}
        />
      }
      sidebar={
        <Sidebar
          items={NAV_ITEMS}
          activeItemId="home"
          crewName={crewLabel}
          roleName={project.roleName}
          projectTimeLabel={formatLocalTimeZoneLabel(currentUser.loadedAt)}
        />
      }
    >
      <HomePage
        projectName={project.name}
        crewName={crewLabel}
        roleName={project.roleName}
        todayLabel={formatTodayLabel(currentUser.loadedAt)}
        canReportDelay={project.canReportDelay}
        advanceNotice={ADVANCE_NOTICE}
        metrics={HOME_METRICS}
        attentionDelays={ATTENTION_DELAYS}
        activityEntries={ACTIVITY_ENTRIES}
        lastCheckedLabel={LAST_CHECKED_LABEL}
      />
    </AppShell>
  )
}

export default App
