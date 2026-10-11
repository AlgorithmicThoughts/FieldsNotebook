import type { ReactNode } from 'react'
import { Button } from '@/components/common/Button'
import { ChoiceGroup } from '@/components/common/ChoiceGroup'
import { FormField, FormFieldset } from '@/components/common/Field'
import { TextArea, TextInput } from '@/components/common/Input'
import { SelectField } from '@/components/common/Select'
import { DELAY_ACKNOWLEDGMENT_CHOICES, type DelayDetail } from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import { needsCompletionTime, verifiableImpacts, type DelayActionDraft } from '../delayActionDraft'
import type { DelayActionId } from '../trackingView'

/** The button that sends each action, in the words of the action itself. */
const CONFIRM_LABEL: Record<DelayActionId, string> = {
  acknowledge: 'Acknowledge',
  completionTime: 'Save completion time',
  requestCheck: 'Request handover check',
  verify: 'Verify handover',
  resolve: 'Resolve',
  reopen: 'Reopen delay',
  revoke: 'Withdraw delay',
}

interface DelayActionFieldsProps {
  action: DelayActionId
  delay: DelayDetail
  user: CurrentUser
  draft: DelayActionDraft
  disabled: boolean
  onDraftChange: (patch: Partial<DelayActionDraft>) => void
}

function AcknowledgeFields({ draft, disabled, onDraftChange }: DelayActionFieldsProps) {
  return (
    <>
      <FormFieldset
        name="acknowledgment-choice"
        legend="Your answer"
        required
        description="Speak to your Foreman first. The answer reaches the affected crew's Superintendent."
      >
        <ChoiceGroup
          label="Your answer"
          value={draft.choice}
          onValueChange={(choice) => onDraftChange({ choice })}
          choices={DELAY_ACKNOWLEDGMENT_CHOICES}
        />
      </FormFieldset>

      {needsCompletionTime(draft) ? (
        <FormField
          name="acknowledgment-ready-at"
          label="Tentative completion"
          required
          description="An estimate only. The time alone never clears the blocker."
        >
          <TextInput
            type="datetime-local"
            value={draft.expectedReadyAt}
            disabled={disabled}
            onChange={(event) => onDraftChange({ expectedReadyAt: event.target.value })}
          />
        </FormField>
      ) : null}

      <FormField name="acknowledgment-note" label="Note" description="Optional.">
        <TextArea
          value={draft.note}
          disabled={disabled}
          onChange={(event) => onDraftChange({ note: event.target.value })}
        />
      </FormField>
    </>
  )
}

function CompletionTimeFields({ draft, disabled, onDraftChange }: DelayActionFieldsProps) {
  return (
    <FormField
      name="revised-ready-at"
      label="Revised completion"
      required
      description="This updates the current cycle and records both times in the history."
    >
      <TextInput
        type="datetime-local"
        value={draft.expectedReadyAt}
        disabled={disabled}
        onChange={(event) => onDraftChange({ expectedReadyAt: event.target.value })}
      />
    </FormField>
  )
}

function VerifyFields({ delay, user, draft, disabled, onDraftChange }: DelayActionFieldsProps) {
  const impacts = verifiableImpacts(delay, user)

  if (impacts.length === 0) {
    return <p>Your crew already verified the handover in this cycle. A reopen asks for it again.</p>
  }

  return (
    <>
      {impacts.length > 1 ? (
        <FormField
          name="verify-impact"
          label="Affected crew"
          required
          description="Each affected work item needs its own check in this cycle."
        >
          <SelectField
            value={draft.impactId === null ? null : String(draft.impactId)}
            onValueChange={(value) => onDraftChange({ impactId: value === null ? null : Number(value) })}
            options={impacts.map((impact) => ({
              value: String(impact.id),
              label:
                impact.workItemTitle === null
                  ? impact.crewName
                  : `${impact.crewName} · ${impact.workItemTitle}`,
            }))}
            placeholder="Choose the crew you are verifying for"
            disabled={disabled}
          />
        </FormField>
      ) : null}

      <FormField
        name="verify-note"
        label="Note"
        description="Optional. Check the finished work on site before you verify."
      >
        <TextArea
          value={draft.note}
          disabled={disabled}
          onChange={(event) => onDraftChange({ note: event.target.value })}
        />
      </FormField>
    </>
  )
}

function ReopenFields({ draft, disabled, onDraftChange }: DelayActionFieldsProps) {
  return (
    <FormField
      name="reopen-reason"
      label="Why is this reopening?"
      required
      description="A reopen starts a new cycle. It needs another acknowledgment and new handover checks."
    >
      <TextArea
        value={draft.reason}
        disabled={disabled}
        onChange={(event) => onDraftChange({ reason: event.target.value })}
      />
    </FormField>
  )
}

function RevokeFields() {
  return (
    <p>
      Withdrawing closes this delay for everyone. Field Notebook has no approved way to bring a
      withdrawn delay back.
    </p>
  )
}

/** Only these actions collect values; the rest send on the first click. */
const ACTION_FIELDS: Partial<Record<DelayActionId, (props: DelayActionFieldsProps) => ReactNode>> = {
  acknowledge: AcknowledgeFields,
  completionTime: CompletionTimeFields,
  verify: VerifyFields,
  reopen: ReopenFields,
  revoke: RevokeFields,
}

interface DelayActionFormProps extends Omit<DelayActionFieldsProps, 'disabled'> {
  pending: boolean
  onSubmit: () => void
  onCancel: () => void
}

/** The values one action needs, with the control that sends it. */
export function DelayActionForm({ pending, onSubmit, onCancel, ...rest }: DelayActionFormProps) {
  const Fields = ACTION_FIELDS[rest.action]

  return (
    <div className="action-form">
      {Fields === undefined ? null : <Fields {...rest} disabled={pending} />}

      <Button
        label={pending ? 'Saving…' : CONFIRM_LABEL[rest.action]}
        variant="primary"
        disabled={pending}
        onClick={onSubmit}
      />
      <Button label="Cancel" variant="quiet" disabled={pending} onClick={onCancel} />
    </div>
  )
}
