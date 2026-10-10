import type { ReactNode } from 'react'
import {
  Card as ShadcnCard,
  CardAction,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'

/** Approved panel metrics: 16 px radius, a single border, and no inner gap. */
const APPROVED_SHAPE =
  'gap-0 rounded-[var(--radius-card)] border border-border bg-card p-0 text-[15px] ring-0'

interface CardProps {
  children: ReactNode
  className?: string
}

export function Card({ children, className }: CardProps) {
  return (
    <ShadcnCard className={`${APPROVED_SHAPE} ${className ?? ''}`}>{children}</ShadcnCard>
  )
}

interface CardHeadingProps {
  title: string
  /** Trailing control or status, such as a link button or a label. */
  action?: ReactNode
}

export function CardHeading({ title, action }: CardHeadingProps) {
  return (
    <CardHeader className="items-center gap-3 px-[22px] pt-5 pb-4">
      <CardTitle>
        <h2>{title}</h2>
      </CardTitle>
      {action ? <CardAction className="self-center">{action}</CardAction> : null}
    </CardHeader>
  )
}
