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
