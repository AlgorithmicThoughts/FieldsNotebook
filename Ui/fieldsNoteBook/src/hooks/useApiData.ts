import { useEffect, useState } from 'react'
import { apiGet } from '@/api/client'

export interface ApiData<T> {
  data: T | undefined
  error: Error | undefined
  /** Time the current data arrived. */
  loadedAt: Date | undefined
}

interface Result<T> extends ApiData<T> {
  path: string
}

/**
 * Loads one GET resource. A null path loads nothing.
 * Changing the path aborts the previous request, and a late result for an earlier path is never returned.
 */
export function useApiData<T>(path: string | null): ApiData<T> {
  const [result, setResult] = useState<Result<T>>()

  useEffect(() => {
    if (path === null) {
      return
    }
    const controller = new AbortController()
    apiGet<T>(path, controller.signal).then(
      (data) => setResult({ path, data, error: undefined, loadedAt: new Date() }),
      (error: unknown) => {
        if (!controller.signal.aborted) {
          setResult({ path, data: undefined, error: toError(error), loadedAt: undefined })
        }
      },
    )
    return () => controller.abort()
  }, [path])

  return result && result.path === path
    ? result
    : { data: undefined, error: undefined, loadedAt: undefined }
}

function toError(error: unknown): Error {
  return error instanceof Error ? error : new Error(String(error))
}
