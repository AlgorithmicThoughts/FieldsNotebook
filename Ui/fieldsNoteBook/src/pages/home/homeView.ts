import { CirclePause, ClipboardCheck, Users } from 'lucide-react'
import type { DelaySummary } from '@/domain/delays'
import { canVerifyHandover } from '@/domain/permissions'
import type { CurrentUser } from '@/domain/users'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'
import type { AdvanceNotice } from './components/AdvanceNoticeHero'
import type { HomeMetric } from './components/MetricCard'

/** Rows the "Needs attention" card shows. The metrics count every delay. */
export const ATTENTION_LIMIT = 4

/** Entries the activity timeline shows. */
export const ACTIVITY_LIMIT = 5

/** A delay stays active until it is resolved or withdrawn. */
function isActive(delay: DelaySummary): boolean {
  return delay.statusCode !== 'Resolved' && delay.statusCode !== 'Revoked'
}

function affectsCrew(delay: DelaySummary, user: CurrentUser): boolean {
  return delay.blockedCrewNames.includes(user.crewName)
}

function isOwnedByCrew(delay: DelaySummary, user: CurrentUser): boolean {
  return delay.ownerCrewName === user.crewName
}

/** Counts across every active delay in the project, not only the rows shown. */
export function toHomeMetrics(delays: DelaySummary[], user: CurrentUser): HomeMetric[] {
  const active = delays.filter(isActive)

  return [
    {
      id: 'blocking',
      icon: CirclePause,
      value: active.filter((delay) => affectsCrew(delay, user)).length,
      label: 'Blocking our crew',
    },
    {
      id: 'dependent',
      icon: Users,
      value: active.filter((delay) => isOwnedByCrew(delay, user)).length,
      label: 'Crews waiting on us',
    },
    {
      // Only the affected crew's Foreman verifies a handover.
      id: 'verify',
      icon: ClipboardCheck,
      value: active.filter(
        (delay) => delay.statusCode === 'ReadyForHandover' && canVerifyHandover(user, delay),
      ).length,
      label: 'Ready to verify',
    },
  ]
}

/** Active delays the crew owns or waits on, most pressing first. */
export function toAttentionDelays(delays: DelaySummary[], user: CurrentUser): DelaySummary[] {
  return delays
    .filter(
      (delay) => isActive(delay) && (isOwnedByCrew(delay, user) || affectsCrew(delay, user)),
    )
    .sort((left, right) => {
      const byPriority = Number(isPressing(right)) - Number(isPressing(left))

      return byPriority !== 0
        ? byPriority
        : Date.parse(right.createdAt) - Date.parse(left.createdAt)
    })
    .slice(0, ATTENTION_LIMIT)
}

function isPressing(delay: DelaySummary): boolean {
  return delay.isHighPriority || delay.statusCode === 'Reopened'
}

/**
 * The advance notice the hero announces: the crew's own first, otherwise the
 * project's. A notice without an estimate has no time to show, so it waits for
 * one rather than claiming a readiness the crew has not given.
 */
export function toAdvanceNotice(
  delays: DelaySummary[],
  user: CurrentUser,
  now: Date = new Date(),
): AdvanceNotice | null {
  const candidates = delays.filter(
    (delay) =>
      isActive(delay) && delay.reportType === 'AdvanceNotice' && delay.expectedReadyAt !== null,
  )
  const notice = candidates.find((delay) => affectsCrew(delay, user)) ?? candidates[0]

  if (notice === undefined || notice.expectedReadyAt === null) {
    return null
  }

  return {
    delayNumber: notice.number,
    headline: notice.title,
    detail: `${notice.ownerCrewName} reports ${notice.tagName.toLowerCase()} at ${notice.locationName}.`,
    ownerCrewName: notice.ownerCrewName,
    blockedCrewNames: notice.blockedCrewNames,
    expectedReadyTimeLabel: formatProjectTime(notice.expectedReadyAt),
    expectedReadyDayLabel: formatProjectDayLabel(notice.expectedReadyAt, now),
  }
}
