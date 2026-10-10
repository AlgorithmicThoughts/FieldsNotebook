import type { ReactNode } from 'react'
import type { CurrentUser } from '@/domain/users'
import { AppStateContext, type AppState } from './appState'
import { useUserSettings } from './useUserSettings'
import { useUnreadNotices } from './useUnreadNotices'

interface AppStateProviderProps {
  /** Mounted once the user is known, so saved preferences seed the state directly. */
  user: CurrentUser
  children: ReactNode
}

export function AppStateProvider({ user, children }: AppStateProviderProps) {
  const userSettings = useUserSettings(user)
  const unreadNotices = useUnreadNotices(user)
  const state: AppState = { ...userSettings, ...unreadNotices }

  return <AppStateContext value={state}>{children}</AppStateContext>
}
