import { formatProjectDayLabel } from '@/lib/projectTime'

const DAY_MS = 86_400_000

/** "Tomorrow · 11 October" for the project day this page plans for. */
export function nextProjectDayLabel(now: Date = new Date()): string {
  return formatProjectDayLabel(new Date(now.getTime() + DAY_MS), now)
}
