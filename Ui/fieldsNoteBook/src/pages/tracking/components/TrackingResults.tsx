import { ListTodo } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Card, CardHeading } from '@/components/common/Card'
import { DelayRow } from '@/components/delays/DelayRow'
import { EmptyDelays } from '@/components/delays/EmptyDelays'
import type { TrackingResultsView } from '../trackingView'
import { TrackingSkeleton } from './TrackingSkeleton'

interface TrackingResultsProps {
  view: TrackingResultsView
  onRetry: () => void
  onOpenDelay: (delayNumber: string) => void
}

/** The delays the current scope, tab, and search term leave in the list. */
export function TrackingResults({ view, onRetry, onOpenDelay }: TrackingResultsProps) {
  if (view.kind === 'loading') {
    return <TrackingSkeleton />
  }

  if (view.kind === 'error') {
    return (
      <Card>
        <div className="grid justify-items-start gap-3 p-[22px]" role="alert">
          <h2>Delays are not available</h2>
          <p className="muted">{view.message}</p>
          <Button label="Try again" variant="primary" onClick={onRetry} />
        </div>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeading
        title="Delays in scope"
        action={<Badge label={`${view.delays.length} shown`} tone="outline" icon={ListTodo} />}
      />

      {view.delays.length === 0 ? (
        <EmptyDelays
          title={view.emptyMessage ?? 'No delays match'}
          detail={view.emptyMessage ? 'Tracking records will appear here when available.' : 'Change the scope, the filter, or the search term to see more.'}
        />
      ) : (
        view.delays.map((delay) => (
          <DelayRow key={delay.id} delay={delay} onOpen={onOpenDelay} />
        ))
      )}
    </Card>
  )
}
