import { useMemo, type ReactNode } from 'react'
import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from '@/components/ui/field'
import { FieldControlContext } from './fieldControl'

/** Approved field text: 13 px bold labels and 12 px muted help. */
const APPROVED_LABEL = 'text-[13px] leading-[1.4] font-bold'
const APPROVED_HELP = 'text-[12px] leading-[1.4]'

interface FieldTextProps {
  name: string
  description?: ReactNode
  error?: string
}

function describedBy({ name, description, error }: FieldTextProps): string | undefined {
  const ids = [description ? `${name}-description` : null, error ? `${name}-error` : null].filter(
    (id) => id !== null,
  )

  return ids.length ? ids.join(' ') : undefined
}

function FieldText({ name, description, error }: FieldTextProps) {
  return (
    <>
      {description ? (
        <FieldDescription id={`${name}-description`} className={APPROVED_HELP}>
          {description}
        </FieldDescription>
      ) : null}
      <FieldError id={`${name}-error`} className={APPROVED_HELP}>
        {error}
      </FieldError>
    </>
  )
}

interface FormFieldProps extends FieldTextProps {
  label: string
  required?: boolean
  /** Trailing control or count shown beside the label. */
  action?: ReactNode
  className?: string
  children: ReactNode
}

/** One labelled control, with its help text and its error region. */
export function FormField({
  name,
  label,
  required,
  description,
  error,
  action,
  className,
  children,
}: FormFieldProps) {
  const id = `${name}-control`
  const describedById = describedBy({ name, description, error })
  const isInvalid: true | undefined = error ? true : undefined
  const control = useMemo(
    () => ({ id, 'aria-describedby': describedById, 'aria-invalid': isInvalid }),
    [id, describedById, isInvalid],
  )

  return (
    <Field data-invalid={error ? true : undefined} className={className}>
      <FieldControlContext value={control}>
        <div className="flex items-center justify-between gap-3">
          <FieldLabel htmlFor={id} className={`gap-0.5 ${APPROVED_LABEL}`}>
            {label}
            {required ? <span aria-hidden="true">*</span> : null}
          </FieldLabel>
          {action}
        </div>
        {children}
        <FieldText name={name} description={description} error={error} />
      </FieldControlContext>
    </Field>
  )
}

interface FormFieldsetProps extends FieldTextProps {
  legend: string
  required?: boolean
  action?: ReactNode
  className?: string
  children: ReactNode
}

/** A group of related controls, such as a radio group or a checkbox list. */
export function FormFieldset({
  name,
  legend,
  required,
  description,
  error,
  action,
  className,
  children,
}: FormFieldsetProps) {
  return (
    <FieldSet data-invalid={error ? true : undefined} className={`gap-2 ${className ?? ''}`}>
      <div className="flex items-center justify-between gap-3">
        <FieldLegend variant="label" className={`mb-0 ${APPROVED_LABEL}`}>
          {legend}
          {required ? <span aria-hidden="true" className="ml-0.5">*</span> : null}
        </FieldLegend>
        {action}
      </div>
      {children}
      <FieldText name={name} description={description} error={error} />
    </FieldSet>
  )
}
