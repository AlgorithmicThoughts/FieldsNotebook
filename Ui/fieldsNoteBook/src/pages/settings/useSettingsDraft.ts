import { useState } from 'react'
import { isApiError } from '@/api/errors'
import type { UserSettings, UserSettingsUpdate } from '@/api/users'

type SettingKey = keyof UserSettings

export interface SettingsDraftState {
  /** What the controls show: the saved row with any unsaved edits over it. */
  values: UserSettings
  hasChanges: boolean
  isSaving: boolean
  /** Message from the last failed save, cleared by the next edit or save. */
  saveError: string | null
  change: <K extends SettingKey>(key: K, value: UserSettings[K]) => void
  save: () => void
}

const SAVE_FAILED = 'Field Notebook could not save your settings.'

/**
 * The draft fields whose value differs from `other`. Against the saved row
 * these are the changes to send; against a finished save they are the edits
 * made while it was out.
 */
function differingFrom(draft: UserSettingsUpdate, other: UserSettingsUpdate): UserSettingsUpdate {
  const entries = Object.entries(draft).filter(([key, value]) => other[key as SettingKey] !== value)

  return Object.fromEntries(entries) as UserSettingsUpdate
}

/**
 * Holds Settings page edits until the user saves, then sends them together.
 * Nothing applies, the theme included, until the save succeeds.
 */
export function useSettingsDraft(
  saved: UserSettings,
  saveSettings: (update: UserSettingsUpdate) => Promise<void>,
): SettingsDraftState {
  const [draft, setDraft] = useState<UserSettingsUpdate>({})
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const changes = differingFrom(draft, saved)
  const hasChanges = Object.keys(changes).length > 0

  function change<K extends SettingKey>(key: K, value: UserSettings[K]) {
    setDraft((current) => ({ ...current, [key]: value }))
    setSaveError(null)
  }

  function save() {
    if (!hasChanges || isSaving) {
      return
    }

    setIsSaving(true)
    setSaveError(null)

    saveSettings(changes)
      .then(() => setDraft((current) => differingFrom(current, changes)))
      .catch((error: unknown) => setSaveError(isApiError(error) ? error.message : SAVE_FAILED))
      .finally(() => setIsSaving(false))
  }

  return { values: { ...saved, ...draft }, hasChanges, isSaving, saveError, change, save }
}
