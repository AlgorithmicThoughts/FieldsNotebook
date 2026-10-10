import { Skeleton } from '@/components/ui/skeleton'

/** The blocked-work rows while the page loads: the real slots, without content. */
export function UpcomingSkeleton() {
  return (
    <div className="grid gap-4 p-[22px]" aria-busy="true">
      {[0, 1, 2].map((slot) => (
        <Skeleton key={slot} className="h-[96px] w-full" />
      ))}
    </div>
  )
}
