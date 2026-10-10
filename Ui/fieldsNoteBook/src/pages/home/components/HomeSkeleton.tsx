import { Card } from '@/components/common/Card'
import { Skeleton } from '@/components/ui/skeleton'
import '../HomePage.css'

/** Home while the delays load: the real slots, without their content. */
export function HomeSkeleton() {
  return (
    <div aria-busy="true">
      <div className="metrics">
        {[0, 1, 2].map((slot) => (
          <Skeleton key={slot} className="h-[92px] w-full rounded-[var(--radius-card)]" />
        ))}
      </div>

      <div className="dashboard-grid">
        <Card>
          <div className="grid gap-3 p-[22px]">
            <Skeleton className="h-6 w-40" />
            {[0, 1, 2, 3].map((slot) => (
              <Skeleton key={slot} className="h-16 w-full" />
            ))}
          </div>
        </Card>

        <aside className="stack">
          <Card>
            <div className="grid gap-3 p-[22px]">
              <Skeleton className="h-6 w-40" />
              {[0, 1, 2].map((slot) => (
                <Skeleton key={slot} className="h-12 w-full" />
              ))}
            </div>
          </Card>
        </aside>
      </div>
    </div>
  )
}
