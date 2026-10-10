import { useState } from 'react'
import { CURRENT_USER_PATH, type CurrentUser } from './api/currentUser'
import { StartupStatus } from './components/layout/StartupStatus'
import { pickDefaultProject } from './domain/users'
import { useApiData } from './hooks/useApiData'
import { useResolvedTheme, type ThemePreference } from './hooks/useResolvedTheme'
import { ProjectWorkspace } from './ProjectWorkspace'

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

  return (
    <ProjectWorkspace
      user={user}
      project={project}
      loadedAt={currentUser.loadedAt}
      theme={theme}
      onThemeToggle={() => setThemePreference(theme === 'dark' ? 'light' : 'dark')}
    />
  )
}

export default App
