import { Card } from '@/components/common/Card'
import { Skeleton } from '@/components/ui/skeleton'

/** The results card while the delays load: the real slots, without content. */
export function TrackingSkeleton() {
  return (
    <div aria-busy="true">
      <Card>
        <div className="grid gap-3 p-[22px]">
          <Skeleton className="h-6 w-48" />
          {[0, 1, 2, 3, 4].map((slot) => (
            <Skeleton key={slot} className="h-16 w-full" />
          ))}
        </div>
      </Card>
    </div>
  )
}
