import { useEffect, useState } from 'react'

/**
 * Holds a value back until it stops changing, so typing a year into a date
 * input fires one request rather than one per keystroke.
 */
export function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [settled, setSettled] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setSettled(value), delayMs)

    return () => clearTimeout(timer)
  }, [value, delayMs])

  return settled
}
