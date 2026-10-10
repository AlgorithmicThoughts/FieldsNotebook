import { Skeleton } from '@/components/ui/skeleton'

/** The notice rows while the list loads: the real slots, without content. */
export function NoticesSkeleton() {
  return (
    <div className="grid gap-4 p-[22px]" aria-busy="true">
      {[0, 1, 2, 3].map((slot) => (
        <Skeleton key={slot} className="h-[84px] w-full" />
      ))}
    </div>
  )
}
