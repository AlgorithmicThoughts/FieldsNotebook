import { CirclePause, ClipboardCheck, Users } from 'lucide-react'
import type { AdvanceNoticeDto, ProjectHomeMetrics } from '@/api/home'
import { formatRelativeDayLabel, formatTimeLabel } from '@/domain/time'
import type { AdvanceNotice } from './components/AdvanceNoticeHero'
import type { HomeMetric } from './components/MetricCard'

/** The three home counts in their approved order. */
export function toHomeMetrics(metrics: ProjectHomeMetrics): HomeMetric[] {
  return [
    { id: 'blocking', icon: CirclePause, value: metrics.blockingOurCrews, label: 'Blocking our crew' },
    { id: 'dependent', icon: Users, value: metrics.crewsWaitingOnUs, label: 'Crews waiting on us' },
    { id: 'verify', icon: ClipboardCheck, value: metrics.readyToVerify, label: 'Ready to verify' },
  ]
}

/** Local-time labels for the tentative completion; an estimate is never shown as clearance. */
export function toAdvanceNotice(notice: AdvanceNoticeDto, now: Date): AdvanceNotice {
  const readyAt = notice.expectedReadyAt ? new Date(notice.expectedReadyAt) : null

  return {
    delayNumber: notice.delayNumber,
    headline: notice.title,
    detail: notice.description,
    ownerCrewName: notice.ownerCrewName,
    blockedCrewNames: notice.blockedCrewNames,
    expectedReadyTimeLabel: readyAt ? formatTimeLabel(readyAt) : 'Awaiting estimate',
    expectedReadyDayLabel: readyAt
      ? formatRelativeDayLabel(readyAt, now)
      : 'The responsible Superintendent has not given a time.',
  }
}
