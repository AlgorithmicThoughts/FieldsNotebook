import type { ComponentProps } from 'react'
import type { LucideIcon } from 'lucide-react'
import { Button as ShadcnButton } from '@/components/ui/button'
import { Icon } from './Icon'

type ButtonVariant = 'default' | 'primary' | 'quiet'

/** Approved control metrics: 44 px minimum height, 10 px radius, and 20 px icons. */
const APPROVED_SHAPE =
  "h-auto min-h-11 gap-2 rounded-[var(--radius-control)] px-4 py-2.5 text-sm font-[650] [&_svg:not([class*='size-'])]:size-5"

const VARIANT_SHAPE: Record<ButtonVariant, string> = {
  default: 'border-border bg-[var(--raised)] text-foreground hover:border-[var(--control)]',
  primary: 'bg-primary text-primary-foreground hover:bg-[var(--pressed)]',
  quiet: 'text-primary hover:bg-transparent',
}

const VARIANT_BASE: Record<ButtonVariant, ComponentProps<typeof ShadcnButton>['variant']> = {
  default: 'outline',
  primary: 'default',
  quiet: 'ghost',
}

interface ButtonProps extends Omit<ComponentProps<typeof ShadcnButton>, 'variant' | 'children'> {
  label: string
  variant?: ButtonVariant
  icon?: LucideIcon
  iconPosition?: 'start' | 'end'
}

export function Button({
  label,
  variant = 'default',
  icon,
  iconPosition = 'start',
  className,
  ...rest
}: ButtonProps) {
  const glyph = icon ? <Icon icon={icon} /> : null

  return (
    <ShadcnButton
      variant={VARIANT_BASE[variant]}
      className={`${APPROVED_SHAPE} ${VARIANT_SHAPE[variant]} ${className ?? ''}`}
      {...rest}
    >
      {iconPosition === 'start' ? glyph : null}
      {label}
      {iconPosition === 'end' ? glyph : null}
    </ShadcnButton>
  )
}

interface IconButtonProps extends Omit<ComponentProps<typeof ShadcnButton>, 'children'> {
  /** Accessible name for a control that shows only an icon. */
  label: string
  icon: LucideIcon
}

export function IconButton({ label, icon, className, ...rest }: IconButtonProps) {
  return (
    <ShadcnButton
      variant="ghost"
      size="icon"
      aria-label={label}
      className={`relative size-11 rounded-[var(--radius-control)] text-foreground [&_svg:not([class*='size-'])]:size-5 ${className ?? ''}`}
      {...rest}
    >
      <Icon icon={icon} />
    </ShadcnButton>
  )
}
