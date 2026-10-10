import { Skeleton } from '@/components/ui/skeleton'

/** The preview column while the report loads: the real slots, without content. */
export function ReportSkeleton() {
  return (
    <div className="grid gap-4 p-[22px]" aria-busy="true">
      <div className="report-tiles">
        <Skeleton className="h-[78px] w-full rounded-[14px]" />
        <Skeleton className="h-[78px] w-full rounded-[14px]" />
      </div>
      {[0, 1, 2, 3].map((slot) => (
        <Skeleton key={slot} className="h-20 w-full" />
      ))}
    </div>
  )
}
