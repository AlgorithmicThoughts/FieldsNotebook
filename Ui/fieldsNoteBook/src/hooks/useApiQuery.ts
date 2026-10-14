import { useEffect, useEffectEvent, useState } from 'react'
import { errorMessage, isAbortError } from '@/api/errors'

/** One GET as a page sees it. `idle` means the caller has nothing to ask for yet. */
export type ApiQueryState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: T }

export interface ApiQuery<T> {
  state: ApiQueryState<T>
  /** Asks again. Loaded data stays on screen until the answer arrives; an error gives way to loading. */
  reload: () => void
  /** Patches the loaded data in place, such as a delay the user just raised. */
  update: (change: (data: T) => T) => void
}

type Settled<T> = { key: string } & ({ status: 'error'; message: string } | { status: 'ready'; data: T })

/**
 * The one place a view loads from the API. `key` names what the view needs, such
 * as `project:7`; a new key aborts the previous request, and a late answer for
 * an old key never renders. A null key loads nothing.
 */
export function useApiQuery<T>(
  key: string | null,
  load: (signal: AbortSignal) => Promise<T>,
  fallbackMessage: string,
): ApiQuery<T> {
  const [settled, setSettled] = useState<Settled<T> | null>(null)
  const [attempt, setAttempt] = useState(0)
  // An Effect Event reads the latest `load` without restarting the request on every render.
  const runLoad = useEffectEvent(load)

  useEffect(() => {
    if (key === null) {
      return
    }

    const controller = new AbortController()
    const { signal } = controller

    runLoad(signal)
      .then((data) => {
        if (!signal.aborted) {
          setSettled({ key, status: 'ready', data })
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return
        }

        setSettled({ key, status: 'error', message: errorMessage(error, fallbackMessage) })
      })

    return () => controller.abort()
  }, [key, attempt, fallbackMessage])

  function reload() {
    setSettled((current) => (current?.status === 'error' ? null : current))
    setAttempt((count) => count + 1)
  }

  function update(change: (data: T) => T) {
    setSettled((current) => (current?.status === 'ready' ? { ...current, data: change(current.data) } : current))
  }

  return { state: toState(key, settled), reload, update }
}

function toState<T>(key: string | null, settled: Settled<T> | null): ApiQueryState<T> {
  if (key === null) {
    return { status: 'idle' }
  }

  if (settled === null || settled.key !== key) {
    return { status: 'loading' }
  }

  return settled.status === 'ready'
    ? { status: 'ready', data: settled.data }
    : { status: 'error', message: settled.message }
}
