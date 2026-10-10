import type { ComponentProps } from 'react'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { useFieldControl } from './fieldControl'

/**
 * Approved control metrics: 44 px minimum height, 10 px radius, 15 px text, and
 * the `--input` boundary the primitive already applies. The generated sizes are
 * smaller, so the important modifier keeps the approved values.
 */
const APPROVED_CONTROL =
  'h-auto min-h-11 w-full rounded-[var(--radius-control)] bg-[var(--card)]! px-3 py-2.5 text-[15px]!'

export function TextInput({ className, ...rest }: ComponentProps<typeof Input>) {
  const field = useFieldControl()

  return <Input {...field} {...rest} className={`${APPROVED_CONTROL} ${className ?? ''}`} />
}

export function TextArea({ className, ...rest }: ComponentProps<typeof Textarea>) {
  const field = useFieldControl()

  return (
    <Textarea
      {...field}
      {...rest}
      className={`${APPROVED_CONTROL} min-h-[116px] resize-y ${className ?? ''}`}
    />
  )
}
