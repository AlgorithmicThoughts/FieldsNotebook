import { CirclePause, ClipboardCheck, Users } from 'lucide-react'
import type { HomeMetricCounts } from '@/api/home'
import type { DelaySummary } from '@/domain/delays'
import { formatProjectDayLabel, formatProjectTime } from '@/lib/projectTime'
import type { AdvanceNotice } from './components/AdvanceNoticeHero'
import type { HomeMetric } from './components/MetricCard'

/**
 * Presentation only. The API decides which delays count, which are pressing,
 * and which advance notice leads; Home turns those answers into cards.
 */

export function toHomeMetrics(counts: HomeMetricCounts): HomeMetric[] {
  return [
    { id: 'blocking', icon: CirclePause, value: counts.blockingOurCrews, label: 'Blocking our crew' },
    { id: 'dependent', icon: Users, value: counts.crewsWaitingOnUs, label: 'Crews waiting on us' },
    { id: 'verify', icon: ClipboardCheck, value: counts.readyToVerify, label: 'Ready to verify' },
  ]
}

/**
 * The hero for the leading advance notice. A notice without an estimate has no
 * time to show, so it waits for one rather than claiming a readiness the crew
 * has not given.
 */
export function toAdvanceNotice(notice: DelaySummary | null, now: Date = new Date()): AdvanceNotice | null {
  if (notice === null || notice.expectedReadyAt === null) {
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
