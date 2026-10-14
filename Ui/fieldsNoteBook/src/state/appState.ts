import { createContext, use } from 'react'
import type { UserSettingsState } from './useUserSettings'
import type { UnreadNoticesState } from './useUnreadNotices'

/**
 * Client state that the shell and several pages share for the signed-in user.
 *
 * One concern contributes one hook and one state interface. Adding a concern
 * means writing its hook, widening this union, and spreading the hook result
 * into the provider value; existing concerns do not change.
 */
export type AppState = UserSettingsState & UnreadNoticesState

export const AppStateContext = createContext<AppState | null>(null)

export function useAppState(): AppState {
  const state = use(AppStateContext)

  if (state === null) {
    throw new Error('useAppState needs an AppStateProvider above it.')
  }

  return state
}
