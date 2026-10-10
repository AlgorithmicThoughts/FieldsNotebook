import type { ReactNode } from 'react'
import { Checkbox } from '@/components/ui/checkbox'

/** Approved check row: 44 px tall, a 19 px box, and 13 px text. */
const APPROVED_ROW = 'flex min-h-11 items-center gap-3 py-2.5 text-[13px] leading-[1.4]'
const APPROVED_BOX = 'size-[19px] rounded-[5px] [&>span>svg]:size-4'

interface CheckboxRowProps {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  label: ReactNode
  /** Secondary line under the label. */
  detail?: ReactNode
  disabled?: boolean
}

export function CheckboxRow({
  checked,
  onCheckedChange,
  label,
  detail,
  disabled,
}: CheckboxRowProps) {
  return (
    <label className={APPROVED_ROW}>
      <Checkbox
        checked={checked}
        onCheckedChange={onCheckedChange}
        disabled={disabled}
        className={APPROVED_BOX}
      />
      <span>
        <span className="font-[650]">{label}</span>
        {detail ? <span className="block text-[12px] text-muted-foreground">{detail}</span> : null}
      </span>
    </label>
  )
}
