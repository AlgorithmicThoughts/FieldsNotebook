import { useState } from 'react'
import { AppShell } from './components/layout/AppShell'
import { Sidebar } from './components/layout/Sidebar'
import { TopBar } from './components/layout/TopBar'
import { useResolvedTheme, type ThemePreference } from './hooks/useResolvedTheme'
import { HomePage } from './pages/home/HomePage'
import {
  ACTIVITY_ENTRIES,
  ADVANCE_NOTICE,
  ATTENTION_DELAYS,
  CURRENT_PROJECT,
  CURRENT_USER,
  HAS_UNREAD_NOTIFICATIONS,
  HOME_METRICS,
  LAST_CHECKED_LABEL,
  NAV_ITEMS,
  TODAY_LABEL,
} from './pages/home/homeData'

function App() {
  const [themePreference, setThemePreference] = useState<ThemePreference>('system')
  const theme = useResolvedTheme(themePreference)

  return (
    <AppShell
      header={
        <TopBar
          projectName={CURRENT_PROJECT.name}
          projectCode={CURRENT_PROJECT.code}
          projectPlace={CURRENT_PROJECT.place}
          userDisplayName={CURRENT_USER.displayName}
          userInitials={CURRENT_USER.initials}
          userRoleName={CURRENT_USER.roleName}
          userCrewName={CURRENT_USER.crewName}
          theme={theme}
          hasUnreadNotifications={HAS_UNREAD_NOTIFICATIONS}
          onThemeToggle={() => setThemePreference(theme === 'dark' ? 'light' : 'dark')}
        />
      }
      sidebar={
        <Sidebar
          items={NAV_ITEMS}
          activeItemId="home"
          crewName={CURRENT_USER.crewName}
          roleName={CURRENT_USER.roleName}
          projectTimeLabel={CURRENT_PROJECT.timeLabel}
        />
      }
    >
      <HomePage
        projectName={CURRENT_PROJECT.name}
        crewName={CURRENT_USER.crewName}
        roleName={CURRENT_USER.roleName}
        todayLabel={TODAY_LABEL}
        canReportDelay={CURRENT_USER.canReportDelay}
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
