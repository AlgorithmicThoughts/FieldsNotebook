import { useEffect, useLayoutEffect, useState } from 'react'

export type ThemePreference = 'system' | 'light' | 'dark'
export type ResolvedTheme = 'light' | 'dark'

const DARK_QUERY = '(prefers-color-scheme: dark)'

const systemTheme = (): ResolvedTheme => (window.matchMedia(DARK_QUERY).matches ? 'dark' : 'light')

/**
 * Resolves the System, Light, and Dark preference and applies it to the document.
 * The browser preference applies only while the preference is System.
 */
export function useResolvedTheme(preference: ThemePreference): ResolvedTheme {
  const [browserTheme, setBrowserTheme] = useState<ResolvedTheme>(systemTheme)

  useEffect(() => {
    const query = window.matchMedia(DARK_QUERY)
    const sync = (event: MediaQueryListEvent) => setBrowserTheme(event.matches ? 'dark' : 'light')

    query.addEventListener('change', sync)
    return () => query.removeEventListener('change', sync)
  }, [])

  const resolved = preference === 'system' ? browserTheme : preference

  useLayoutEffect(() => {
    document.documentElement.dataset.theme = resolved
  }, [resolved])

  return resolved
}
