import { Radio } from '@base-ui/react/radio'
import type { LucideIcon } from 'lucide-react'
import { RadioGroup } from '@/components/ui/radio-group'
import { Icon } from './Icon'

/**
 * Approved choice chip: 44 px tall, a 9 px radius, the --control boundary, and
 * the soft accent fill when chosen. The radio keeps the group role and roving
 * focus; the chip shows the focus ring on its behalf. The radio is the bare
 * primitive because the styled RadioGroupItem's size and position override
 * sr-only, which left a hidden 16 px box pushing the chip contents off centre.
 * Base UI also renders a position: fixed native input. Inside a modal it sits at
 * the dialog's corner, and focusing it on click scrolled the whole dialog, so
 * layout containment keeps that input inside its own chip.
 */
const APPROVED_CHOICE =
  'relative flex min-h-11 cursor-pointer items-center justify-center gap-2 [contain:layout] rounded-[9px] border border-input bg-[var(--surface)] px-[13px] py-2.5 text-[13px] leading-[1.4] font-[600] text-foreground has-data-checked:border-[var(--accent)] has-data-checked:bg-[var(--soft)] has-data-checked:text-[var(--accent)] has-[:focus-visible]:outline has-[:focus-visible]:outline-[3px] has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-[var(--accent)]'

export interface Choice<T extends string> {
  value: T
  label: string
  icon?: LucideIcon
}

interface ChoiceGroupProps<T extends string> {
  /** Accessible name for the group of choices. */
  label: string
  value: T
  onValueChange: (value: T) => void
  choices: Choice<T>[]
}

/** A single-choice set shown as the approved chips. */
export function ChoiceGroup<T extends string>({
  label,
  value,
  onValueChange,
  choices,
}: ChoiceGroupProps<T>) {
  return (
    <RadioGroup
      aria-label={label}
      value={value}
      onValueChange={(next) => onValueChange(String(next) as T)}
      className="flex flex-row flex-wrap gap-2"
    >
      {choices.map((choice) => (
        <label key={choice.value} className={APPROVED_CHOICE}>
          <Radio.Root value={choice.value} className="sr-only" />
          {choice.icon ? <Icon icon={choice.icon} /> : null}
          {choice.label}
        </label>
      ))}
    </RadioGroup>
  )
}
