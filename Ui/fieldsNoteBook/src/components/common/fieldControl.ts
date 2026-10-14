import { createContext, use } from 'react'

/** What a labelled control inside a `FormField` needs to be announced right. */
export interface FieldControlAttributes {
  id: string
  'aria-describedby': string | undefined
  'aria-invalid': true | undefined
}

export const FieldControlContext = createContext<FieldControlAttributes | null>(null)

/**
 * `FormField` wires the label, description, and error association once so each
 * control does not repeat it. A control used outside one gets nothing.
 */
export function useFieldControl(): Partial<FieldControlAttributes> {
  return use(FieldControlContext) ?? {}
}
