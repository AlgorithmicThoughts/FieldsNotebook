/**
 * The project runs on one site clock. Every timestamp the API returns is UTC,
 * so the screens format in project time and never in the browser's zone.
 */
export const PROJECT_TIME_ZONE = 'America/Chicago'

export const PROJECT_TIME_LABEL = 'Project time · Central'

const dayName = new Intl.DateTimeFormat('en-GB', {
  timeZone: PROJECT_TIME_ZONE,
  weekday: 'long',
})

const dayAndMonth = new Intl.DateTimeFormat('en-GB', {
  timeZone: PROJECT_TIME_ZONE,
  day: 'numeric',
  month: 'long',
})

const clockTime = new Intl.DateTimeFormat('en-US', {
  timeZone: PROJECT_TIME_ZONE,
  hour: '2-digit',
  minute: '2-digit',
  hour12: true,
})

const calendarDay = new Intl.DateTimeFormat('en-CA', {
  timeZone: PROJECT_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

/** "Wednesday, 7 October" in project time. */
export function formatProjectDate(value: Date | string): string {
  const date = new Date(value)

  return `${dayName.format(date)}, ${dayAndMonth.format(date)}`
}

/** "08:42 AM" in project time. */
export function formatProjectTime(value: Date | string): string {
  return clockTime.format(new Date(value))
}

/** "Today · 7 October", "Tomorrow · 8 October", or "Friday · 9 October". */
export function formatProjectDayLabel(value: Date | string, now: Date = new Date()): string {
  const date = new Date(value)
  const difference = projectDaysBetween(now, date)

  if (difference === 0) {
    return `Today · ${dayAndMonth.format(date)}`
  }

  if (difference === 1) {
    return `Tomorrow · ${dayAndMonth.format(date)}`
  }

  return `${dayName.format(date)} · ${dayAndMonth.format(date)}`
}

/**
 * Whole project-time calendar days from one instant to another. 1 means the
 * next project day; a negative number means the day already passed.
 */
export function projectDaysBetween(from: Date | string, to: Date | string): number {
  const fromDay = Date.parse(`${calendarDay.format(new Date(from))}T00:00:00Z`)
  const toDay = Date.parse(`${calendarDay.format(new Date(to))}T00:00:00Z`)

  return Math.round((toDay - fromDay) / 86_400_000)
}
