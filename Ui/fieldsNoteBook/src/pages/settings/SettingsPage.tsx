import { Button } from '@/components/common/Button'
import { useWorkspace } from '@/routes/useWorkspace'
import { useAppState } from '@/state/appState'
import { AccountCard } from './components/AccountCard'
import { AppearanceCard } from './components/AppearanceCard'
import { NotificationsCard } from './components/NotificationsCard'
import { ProjectsCard } from './components/ProjectsCard'
import { SettingError } from './components/SettingError'
import { useSettingsDraft } from './useSettingsDraft'
import './SettingsPage.css'

/** Preferences and read-only identity. No action here changes a delay. */
export function SettingsPage() {
  const { user, project } = useWorkspace()
  const { savedSettings, themeError, saveSettings } = useAppState()
  const { values, hasChanges, isSaving, saveError, change, save } = useSettingsDraft(
    savedSettings,
    saveSettings,
  )

  return (
    <>
      <div className="page-title">
        <div>
          <h1>Settings</h1>
          <p>Make Field Notebook fit your day.</p>
        </div>
        <Button
          label={isSaving ? 'Saving…' : 'Save'}
          variant="primary"
          disabled={!hasChanges || isSaving}
          onClick={save}
        />
      </div>
      <SettingError message={saveError} />

      <div className="settings-grid">
        <div className="stack">
          <AppearanceCard
            preference={values.themePreference}
            error={themeError}
            onSelect={(preference) => change('themePreference', preference)}
          />
          <NotificationsCard
            email={user.email}
            phone={user.phone}
            emailEnabled={values.emailEnabled}
            smsEnabled={values.smsEnabled}
            onChannelChange={change}
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
