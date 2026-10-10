import { ArrowRight, Plus } from 'lucide-react'
import type { DelaySummary } from '@/domain/delays'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Card, CardHeading } from '@/components/common/Card'
import { ActivityTimeline, type ActivityEntry } from './components/ActivityTimeline'
import { AdvanceNoticeHero, type AdvanceNotice } from './components/AdvanceNoticeHero'
import { DelayRow } from './components/DelayRow'
import { EmptyDelays } from './components/EmptyDelays'
import { HandoverTip } from './components/HandoverTip'
import { MetricCard, type HomeMetric } from './components/MetricCard'
import { RefreshNote } from './components/RefreshNote'
import './HomePage.css'

export interface HomePageProps {
  projectName: string
  crewName: string
  roleName: string
  /** Current project date, such as "Wednesday, 7 October". */
  todayLabel: string
  /** A Foreman of the affected or responsible crew can raise a delay. */
  canReportDelay: boolean
  /** Advance notice for tomorrow, when the project has one. */
  advanceNotice: AdvanceNotice | null
  metrics: HomeMetric[]
  attentionDelays: DelaySummary[]
  activityEntries: ActivityEntry[]
  lastCheckedLabel: string
  onReportDelay?: () => void
  onOpenDelay?: (delayNumber: string) => void
  onOpenMetric?: (metricId: string) => void
  onViewAllDelays?: () => void
}

export function HomePage({
  projectName,
  crewName,
  roleName,
  todayLabel,
  canReportDelay,
  advanceNotice,
  metrics,
  attentionDelays,
  activityEntries,
  lastCheckedLabel,
  onReportDelay,
  onOpenDelay,
  onOpenMetric,
  onViewAllDelays,
}: HomePageProps) {
  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{projectName}</div>
          <h1>Your crew, in sync.</h1>
          <p>
            {crewName} · {roleName} · {todayLabel}
          </p>
        </div>
        {canReportDelay ? (
          <Button label="Report blocker" variant="primary" icon={Plus} onClick={onReportDelay} />
        ) : null}
      </div>

      {advanceNotice ? (
        <AdvanceNoticeHero notice={advanceNotice} onOpenDelay={onOpenDelay} />
      ) : null}

      <div className="metrics">
        {metrics.map((metric) => (
          <MetricCard key={metric.id} metric={metric} onSelect={onOpenMetric} />
        ))}
      </div>

      <div className="dashboard-grid">
        <Card>
          <CardHeading
            title="Needs attention"
            action={
              <Button
                label="All delays"
                variant="quiet"
                icon={ArrowRight}
                iconPosition="end"
                onClick={onViewAllDelays}
              />
            }
          />
          {attentionDelays.length ? (
            attentionDelays.map((delay) => (
              <DelayRow key={delay.number} delay={delay} onOpen={onOpenDelay} />
            ))
          ) : (
            <EmptyDelays
              title="No active delays"
              detail={`${crewName} has no active blockers in this project.`}
            />
          )}
        </Card>

        <aside className="stack">
          <Card>
            <CardHeading title="Latest handovers" action={<Badge label="Today" tone="outline" />} />
            <ActivityTimeline entries={activityEntries} />
          </Card>

          <HandoverTip
            title="A clear handover. A confirmed start."
            detail="The affected crew's Foreman verifies the handover. The responsible Superintendent then resolves the delay."
          />
        </aside>
      </div>

      <RefreshNote lastCheckedLabel={lastCheckedLabel} />
    </>
  )
}
