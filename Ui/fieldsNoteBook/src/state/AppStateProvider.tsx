import type { ReactNode } from 'react'
import type { CurrentUser } from '@/domain/users'
import { AppStateContext, type AppState } from './appState'
import { useThemePreference } from './useThemePreference'
import { useUnreadNotices } from './useUnreadNotices'

interface AppStateProviderProps {
  /** Mounted once the user is known, so saved preferences seed the state directly. */
  user: CurrentUser
  children: ReactNode
}

export function AppStateProvider({ user, children }: AppStateProviderProps) {
  const themePreference = useThemePreference(user)
  const unreadNotices = useUnreadNotices(user)
  const state: AppState = { ...themePreference, ...unreadNotices }

  return <AppStateContext value={state}>{children}</AppStateContext>
}
