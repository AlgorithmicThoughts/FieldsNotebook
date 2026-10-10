import type { ReactNode } from 'react'
import { X } from 'lucide-react'
import { Dialog, DialogClose, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { IconButton } from './Button'

/**
 * Approved dialog metrics: the card surface, a 16 px radius, and the approved
 * shadow. The body scrolls on its own so the page behind stays put.
 */
const APPROVED_SHAPE =
  'grid max-h-[92dvh] grid-rows-[auto_minmax(0,1fr)_auto] gap-0 overflow-hidden rounded-[var(--radius-card)] bg-card p-0 text-[15px] text-foreground shadow-[0_20px_80px_var(--shadow)] ring-1 ring-border'

/** The approved dialog is 650 px, and the wide two-column dialog is 1000 px. */
const APPROVED_WIDTH = {
  default:
    'w-[min(650px,calc(100%-32px))] max-w-[min(650px,calc(100%-32px))] sm:max-w-[min(650px,calc(100%-32px))]',
  wide: 'w-[min(1000px,calc(100%-32px))] max-w-[min(1000px,calc(100%-32px))] sm:max-w-[min(1000px,calc(100%-32px))]',
}

interface ModalProps {
  open: boolean
  title: string
  onOpenChange: (open: boolean) => void
  /** The wider surface a two-column body needs. */
  wide?: boolean
  /** Footer controls, right-aligned. */
  actions?: ReactNode
  children: ReactNode
}

/**
 * A dialog shell with no knowledge of its contents. The parent owns `open`;
 * focus trapping, Escape, the backdrop, and the accessible name come from the
 * primitive.
 */
export function Modal({ open, title, onOpenChange, wide = false, actions, children }: ModalProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={`${APPROVED_SHAPE} ${APPROVED_WIDTH[wide ? 'wide' : 'default']}`}
        showCloseButton={false}
      >
        <DialogHeader className="flex-row items-center justify-between gap-3 border-b border-border px-[22px] py-3">
          <DialogTitle className="text-[19px] leading-[1.4] font-bold tracking-[-0.25px]">
            {title}
          </DialogTitle>
          <DialogClose render={<IconButton label="Close" icon={X} />} />
        </DialogHeader>

        <div className="overflow-y-auto px-[22px] py-5">{children}</div>

        {actions ? (
          <div className="flex flex-wrap items-center justify-end gap-3 border-t border-border px-[22px] py-4">
            {actions}
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
