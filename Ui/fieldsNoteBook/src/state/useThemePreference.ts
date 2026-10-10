import { useRef, useState } from 'react'
import { isApiError } from '@/api/errors'
import { updateUserSettings } from '@/api/users'
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

const SAVE_FAILED = 'Field Notebook could not save your appearance preference.'

export interface ThemePreferenceState {
  /** UserSettings.ThemePreference as the API stores it. */
  themePreference: ThemePreference
  /** The theme now applied to the document, after the System rule resolves. */
  theme: ResolvedTheme
  /** Message from the last failed save, cleared by the next choice. */
  themeError: string | null
  selectThemePreference: (preference: ThemePreference) => void
}

/**
 * Owns the appearance preference for the signed-in user. The choice applies
 * immediately and then persists; a failed save puts the previous choice back
 * rather than letting the screen claim a preference was stored.
 */
export function useThemePreference(user: CurrentUser): ThemePreferenceState {
  const [themePreference, setThemePreference] = useState<ThemePreference>(user.themePreference)
  const [themeError, setThemeError] = useState<string | null>(null)
  const latestSave = useRef(0)
  const theme = useResolvedTheme(RESOLVER_PREFERENCE[themePreference])

  function selectThemePreference(preference: ThemePreference) {
    if (preference === themePreference) {
      return
    }

    const save = latestSave.current + 1
    const previous = themePreference

    latestSave.current = save
    setThemePreference(preference)
    setThemeError(null)

    updateUserSettings(user.id, { themePreference: preference }).catch((error: unknown) => {
      // A later choice already owns the control, so this reply is obsolete.
      if (latestSave.current !== save) {
        return
      }

      setThemePreference(previous)
      setThemeError(isApiError(error) ? error.message : SAVE_FAILED)
    })
  }

  return { themePreference, theme, themeError, selectThemePreference }
}
