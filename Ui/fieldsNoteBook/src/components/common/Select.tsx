import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useFieldControl } from './fieldControl'

/** Approved control metrics: 44 px minimum height, 10 px radius, 15 px text. */
const APPROVED_TRIGGER =
  'h-auto! min-h-11 w-full rounded-[var(--radius-control)] bg-[var(--card)]! px-3 py-2.5 text-[15px] [&_svg:not([class*=size-])]:size-5'

export interface SelectOption {
  value: string
  label: string
}

interface SelectFieldProps {
  value: string | null
  onValueChange: (value: string | null) => void
  options: SelectOption[]
  placeholder: string
  required?: boolean
  disabled?: boolean
  /** Accessible name when no `FormField` label points at the trigger. */
  label?: string
}

export function SelectField({
  value,
  onValueChange,
  options,
  placeholder,
  required,
  disabled,
  label,
}: SelectFieldProps) {
  const field = useFieldControl()

  return (
    <Select
      items={options}
      value={value}
      onValueChange={onValueChange}
      required={required}
      disabled={disabled}
    >
      <SelectTrigger {...field} aria-label={label} className={APPROVED_TRIGGER}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value} className="min-h-11 py-2 text-[15px]">
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
