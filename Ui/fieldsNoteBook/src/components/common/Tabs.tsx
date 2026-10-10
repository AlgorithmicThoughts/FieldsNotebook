import type { ReactNode } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'

/**
 * Approved filter row: a pill set on the card surface rather than underlined
 * tabs. Each pill keeps the 44 px minimum height, and the selected pill uses
 * the soft accent fill with accent text in both themes.
 */
const APPROVED_LIST =
  'h-auto! w-fit max-w-full flex-wrap gap-2 rounded-[var(--radius-card)] border border-border bg-card! p-[6px]'

const APPROVED_TAB =
  'h-auto! min-h-11 flex-none rounded-[9px] border-transparent! bg-transparent! px-[14px] py-2.5 text-[13px] leading-[1.4] font-[650] text-[var(--muted)]! shadow-none! hover:bg-[var(--bg)]! hover:text-[var(--text)]! data-active:bg-[var(--soft)]! data-active:text-[var(--accent)]!'

export interface TabOption<T extends string> {
  value: T
  label: string
}

interface PillTabsProps<T extends string> {
  /** Accessible name for the set of tabs. */
  label: string
  value: T
  onValueChange: (value: T) => void
  options: TabOption<T>[]
  /** Panel the selected tab describes. */
  children: ReactNode
}

/**
 * A single-choice filter row. The primitive supplies the tab roles and
 * arrow-key navigation; one panel always matches the selected tab, so the
 * pills and the content they describe stay associated.
 */
export function PillTabs<T extends string>({
  label,
  value,
  onValueChange,
  options,
  children,
}: PillTabsProps<T>) {
  return (
    <Tabs value={value} onValueChange={(next) => onValueChange(String(next) as T)}>
      <TabsList aria-label={label} className={APPROVED_LIST}>
        {options.map((option) => (
          <TabsTrigger key={option.value} value={option.value} className={APPROVED_TAB}>
            {option.label}
          </TabsTrigger>
        ))}
      </TabsList>

      <TabsContent value={value} className="mt-[18px]">
        {children}
      </TabsContent>
    </Tabs>
  )
}
