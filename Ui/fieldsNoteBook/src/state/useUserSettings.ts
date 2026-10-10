import { useRef, useState } from 'react'
import { isApiError } from '@/api/errors'
import { updateUserSettings, type UserSettings, type UserSettingsUpdate } from '@/api/users'
import type { CurrentUser, ThemePreference } from '@/domain/users'
import {
  useResolvedTheme,
  type ResolvedTheme,
  type ThemePreference as ResolverPreference,
} from '@/hooks/useResolvedTheme'

/** The single boundary between the UserSettings casing and the resolver casing. */
const RESOLVER_PREFERENCE: Record<ThemePreference, ResolverPreference> = {
  System: 'system',
  Light: 'light',
  Dark: 'dark',
}

const THEME_SAVE_FAILED = 'Field Notebook could not save your appearance preference.'

export interface UserSettingsState {
  /** The UserSettings row as last saved, which outlives any one page. */
  savedSettings: UserSettings
  /** UserSettings.ThemePreference as the API stores it. */
  themePreference: ThemePreference
  /** The theme now applied to the document, after the System rule resolves. */
  theme: ResolvedTheme
  /** Message from the last failed quick theme toggle, cleared by the next choice. */
  themeError: string | null
  /** The shell's quick toggle: applies at once, then saves on its own. */
  selectThemePreference: (preference: ThemePreference) => void
  /** Saves several changes in one request; rejects with the API error when it fails. */
  saveSettings: (update: UserSettingsUpdate) => Promise<void>
}

/**
 * Owns the saved preferences for the signed-in user. The Settings page edits a
 * draft and saves it here, so a later visit starts from what was stored rather
 * than from the values the session loaded with.
 */
export function useUserSettings(user: CurrentUser): UserSettingsState {
  const [savedSettings, setSavedSettings] = useState<UserSettings>({
    themePreference: user.themePreference,
    emailEnabled: user.emailEnabled,
    smsEnabled: user.smsEnabled,
  })
  const [themeError, setThemeError] = useState<string | null>(null)
  const latestThemeSave = useRef(0)
  const theme = useResolvedTheme(RESOLVER_PREFERENCE[savedSettings.themePreference])

  function setTheme(themePreference: ThemePreference) {
    setSavedSettings((current) => ({ ...current, themePreference }))
  }

  function selectThemePreference(preference: ThemePreference) {
    if (preference === savedSettings.themePreference) {
      return
    }

    const save = latestThemeSave.current + 1
    const previous = savedSettings.themePreference

    latestThemeSave.current = save
    setTheme(preference)
    setThemeError(null)

    updateUserSettings(user.id, { themePreference: preference }).catch((error: unknown) => {
      // A later choice already owns the theme, so this reply is obsolete.
      if (latestThemeSave.current !== save) {
        return
      }

      setTheme(previous)
      setThemeError(isApiError(error) ? error.message : THEME_SAVE_FAILED)
    })
  }

  async function saveSettings(update: UserSettingsUpdate) {
    const saved = await updateUserSettings(user.id, update)

    setSavedSettings(saved)
    setThemeError(null)
  }

  return {
    savedSettings,
    themePreference: savedSettings.themePreference,
    theme,
    themeError,
    selectThemePreference,
    saveSettings,
  }
}
