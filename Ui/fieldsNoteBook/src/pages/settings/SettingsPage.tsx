import { useWorkspace } from '@/routes/useWorkspace'
import { useAppState } from '@/state/appState'
import { AccountCard } from './components/AccountCard'
import { AppearanceCard } from './components/AppearanceCard'
import { NotificationsCard } from './components/NotificationsCard'
import { ProjectsCard } from './components/ProjectsCard'
import { useNotificationChannels } from './useNotificationChannels'
import './SettingsPage.css'

/** Preferences and read-only identity. No action here changes a delay. */
export function SettingsPage() {
  const { user, project } = useWorkspace()
  const { themePreference, themeError, selectThemePreference } = useAppState()
  const { emailEnabled, smsEnabled, channelError, setChannel } = useNotificationChannels(user)

  return (
    <>
      <div className="settings-title">
        <h1>Settings</h1>
        <p>Make Field Notebook fit your day.</p>
      </div>

      <div className="settings-grid">
        <div className="stack">
          <AppearanceCard
            preference={themePreference}
            error={themeError}
            onSelect={selectThemePreference}
          />
          <NotificationsCard
            email={user.email}
            phone={user.phone}
            emailEnabled={emailEnabled}
            smsEnabled={smsEnabled}
            error={channelError}
            onChannelChange={setChannel}
          />
        </div>

        <div className="stack">
          <AccountCard user={user} projectName={project.name} />
          <ProjectsCard projects={user.projects} activeProjectId={user.activeProjectId} />
        </div>
      </div>
    </>
  )
}
