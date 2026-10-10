import { Switch as ShadcnSwitch } from '@/components/ui/switch'

/** Approved switch metrics: a 46 by 28 px track with a 20 px knob. */
const APPROVED_TRACK =
  'h-7! w-[46px]! border-[var(--control)] px-[3px] data-unchecked:bg-[var(--disabled)]! data-checked:border-[var(--accent)]'
const APPROVED_KNOB =
  '[&>[data-slot=switch-thumb]]:size-5! [&>[data-slot=switch-thumb]]:data-unchecked:bg-[var(--surface)]! [&>[data-slot=switch-thumb]]:data-checked:bg-[var(--on-accent)]!'

interface SwitchProps {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  /** Accessible name, since the visible label sits beside the control. */
  label: string
  disabled?: boolean
}

export function Switch({ checked, onCheckedChange, label, disabled }: SwitchProps) {
  return (
    <ShadcnSwitch
      checked={checked}
      onCheckedChange={onCheckedChange}
      aria-label={label}
      disabled={disabled}
      className={`${APPROVED_TRACK} ${APPROVED_KNOB}`}
    />
  )
}
