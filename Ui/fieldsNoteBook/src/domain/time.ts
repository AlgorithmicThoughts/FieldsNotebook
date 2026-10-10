/** Today's date in the viewer's time zone, such as "Saturday, 10 October". */
export function formatTodayLabel(now: Date): string {
  return now.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' })
}

/** The viewer's time zone, such as "Local time · CDT". The schema stores no project time zone. */
export function formatLocalTimeZoneLabel(now: Date): string {
  const zone = new Intl.DateTimeFormat(undefined, { timeZoneName: 'short' })
    .formatToParts(now)
    .find((part) => part.type === 'timeZoneName')?.value
  return zone ? `Local time · ${zone}` : 'Local time'
}

/** Clock time in the viewer's time zone, such as "4:00 PM". */
export function formatTimeLabel(time: Date): string {
  return time.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
}

/** "Today", "Tomorrow", or the weekday, with the date, such as "Tomorrow · October 11". */
export function formatRelativeDayLabel(time: Date, now: Date): string {
  const date = time.toLocaleDateString(undefined, { day: 'numeric', month: 'long' })
  return `${relativeDayName(time, now)} · ${date}`
}

const MS_PER_DAY = 24 * 60 * 60 * 1000

function relativeDayName(time: Date, now: Date): string {
  const days = Math.round((startOfLocalDay(time) - startOfLocalDay(now)) / MS_PER_DAY)
  if (days === 0) {
    return 'Today'
  }
  if (days === 1) {
    return 'Tomorrow'
  }
  return time.toLocaleDateString(undefined, { weekday: 'long' })
}

function startOfLocalDay(time: Date): number {
  return new Date(time.getFullYear(), time.getMonth(), time.getDate()).getTime()
}

/** "4:00 PM" for today; "October 7 · 9:30 AM" for another day. */
export function formatEventTimeLabel(time: Date, now: Date): string {
  if (startOfLocalDay(time) === startOfLocalDay(now)) {
    return formatTimeLabel(time)
  }
  const date = time.toLocaleDateString(undefined, { day: 'numeric', month: 'long' })
  return `${date} · ${formatTimeLabel(time)}`
}

export function isSameLocalDay(time: Date, now: Date): boolean {
  return startOfLocalDay(time) === startOfLocalDay(now)
}
