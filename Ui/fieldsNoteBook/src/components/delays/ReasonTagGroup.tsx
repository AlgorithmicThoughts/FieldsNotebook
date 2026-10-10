import type { DelayTagOption } from '@/domain/delays'
import { DELAY_TAG_ICON } from '@/domain/delays'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Icon } from '@/components/common/Icon'

/**
 * Approved reason chip: 44 px tall, a 9 px radius, the --control boundary, and
 * the soft accent fill when chosen. The radio itself stays in the accessibility
 * tree; the chip shows the focus ring on its behalf.
 */
const APPROVED_CHIP =
  'flex min-h-11 cursor-pointer items-center gap-[7px] rounded-[9px] border border-input bg-[var(--card)] px-3 py-2.5 text-[12px] leading-[1.4] font-[650] text-foreground has-data-checked:border-[var(--accent)] has-data-checked:bg-[var(--soft)] has-data-checked:text-[var(--accent)] has-[:focus-visible]:outline has-[:focus-visible]:outline-[3px] has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-[var(--accent)]'

interface ReasonTagGroupProps {
  tags: DelayTagOption[]
  value: number | null
  onValueChange: (delayTagId: number) => void
}

/** The nine approved causes. Exactly one applies, and none of them is a priority. */
export function ReasonTagGroup({ tags, value, onValueChange }: ReasonTagGroupProps) {
  return (
    <RadioGroup
      value={value === null ? null : String(value)}
      onValueChange={(next) => onValueChange(Number(next))}
      className="flex flex-row flex-wrap gap-2"
    >
      {tags.map((tag) => (
        <label key={tag.id} className={APPROVED_CHIP}>
          <RadioGroupItem value={String(tag.id)} className="sr-only" />
          <Icon icon={DELAY_TAG_ICON[tag.code]} size={16} />
          {tag.name}
        </label>
      ))}
    </RadioGroup>
  )
}
