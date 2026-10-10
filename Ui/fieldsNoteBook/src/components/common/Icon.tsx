import type { LucideIcon, LucideProps } from 'lucide-react'

interface IconProps extends LucideProps {
  icon: LucideIcon
}

/** Applies the approved 20 px size and 1.8 stroke width to a Lucide icon. */
export function Icon({ icon: Glyph, size = 20, strokeWidth = 1.8, className, ...rest }: IconProps) {
  return (
    <Glyph
      size={size}
      strokeWidth={strokeWidth}
      className={className ? `icon ${className}` : 'icon'}
      aria-hidden="true"
      {...rest}
    />
  )
}
