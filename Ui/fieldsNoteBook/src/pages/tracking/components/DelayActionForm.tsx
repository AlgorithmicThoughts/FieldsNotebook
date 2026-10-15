import { useState, type ReactNode } from 'react'
import { Button } from '@/components/common/Button'
import { ChoiceGroup } from '@/components/common/ChoiceGroup'
import { FormField, FormFieldset } from '@/components/common/Field'
import { TextArea, TextInput } from '@/components/common/Input'
import { SelectField } from '@/components/common/Select'
import { PhotoPicker } from '@/components/delays/PhotoPicker'
import { DELAY_ACKNOWLEDGMENT_CHOICES, type DelayDetail } from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import { needsCompletionTime, verifiableImpacts, type DelayActionDraft } from '../delayActionDraft'
import type { DelayActionId } from '../trackingView'

/** The button that sends each action, in the words of the action itself. */
const CONFIRM_LABEL: Record<DelayActionId, string> = {
  acknowledge: 'Acknowledge',
  completionTime: 'Save completion time',
  requestCheck: 'Submit for review',
  verify: 'Accept for my crew',
  return: 'Send back',
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
  /** True while chosen photos are still being read, so the action cannot send yet. */
  onReadingChange: (reading: boolean) => void
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

/** Only the affected crews this Foreman covers, and only those that have not accepted yet. */
function ImpactChoice({ delay, user, draft, disabled, onDraftChange }: DelayActionFieldsProps) {
  const impacts = verifiableImpacts(delay, user)

  if (impacts.length < 2) {
    return null
  }

  return (
    <FormField
      name="review-impact"
      label="Affected crew"
      required
      description="Each affected crew accepts or sends back for itself."
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
        placeholder="Choose the crew you are reviewing for"
        disabled={disabled}
      />
    </FormField>
  )
}

function VerifyFields(props: DelayActionFieldsProps) {
  const { delay, user, draft, disabled, onDraftChange } = props

  if (verifiableImpacts(delay, user).length === 0) {
    return <p>Your crew already accepted the proof of work in this cycle.</p>
  }

  return (
    <>
      <ImpactChoice {...props} />
      <FormField
        name="verify-note"
        label="Note"
        description="Optional. Accepting closes this delay for your crew only; it closes for everyone once every affected crew accepts."
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

function ReturnFields(props: DelayActionFieldsProps) {
  const { draft, disabled, onDraftChange } = props

  return (
    <>
      <ImpactChoice {...props} />
      <FormField
        name="return-reason"
        label="What must be fixed?"
        required
        description="The delay goes back to the responsible crew, who must fix the work and submit new proof."
      >
        <TextArea
          value={draft.reason}
          disabled={disabled}
          onChange={(event) => onDraftChange({ reason: event.target.value })}
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
      description="A reopen starts a new cycle. It needs another acknowledgment, new proof of work, and a new review."
    >
      <TextArea
        value={draft.reason}
        disabled={disabled}
        onChange={(event) => onDraftChange({ reason: event.target.value })}
      />
    </FormField>
  )
}

function SubmitForReviewFields({ draft, disabled, onDraftChange, onReadingChange }: DelayActionFieldsProps) {
  return (
    <FormFieldset
      name="review-proof-of-work"
      legend="Proof of work"
      required
      description="Show the finished work that removed the blocker. The affected crews review these photos before they accept."
    >
      <PhotoPicker
        photos={draft.photos}
        onChange={(photos) => onDraftChange({ photos })}
        disabled={disabled}
        onReadingChange={onReadingChange}
      />
    </FormFieldset>
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

/** The values each action collects before it sends. */
const ACTION_FIELDS: Record<DelayActionId, (props: DelayActionFieldsProps) => ReactNode> = {
  acknowledge: AcknowledgeFields,
  completionTime: CompletionTimeFields,
  requestCheck: SubmitForReviewFields,
  verify: VerifyFields,
  return: ReturnFields,
  reopen: ReopenFields,
  revoke: RevokeFields,
}

interface DelayActionFormProps extends Omit<DelayActionFieldsProps, 'disabled' | 'onReadingChange'> {
  pending: boolean
  onSubmit: () => void
  onCancel: () => void
}

/** The values one action needs, with the control that sends it. */
export function DelayActionForm({ pending, onSubmit, onCancel, ...rest }: DelayActionFormProps) {
  const Fields = ACTION_FIELDS[rest.action]
  const [isReadingPhotos, setIsReadingPhotos] = useState(false)

  return (
    <div className="action-form">
      <Fields {...rest} disabled={pending} onReadingChange={setIsReadingPhotos} />

      <Button
        label={pending ? 'Saving…' : CONFIRM_LABEL[rest.action]}
        variant="primary"
        disabled={pending || isReadingPhotos}
        onClick={onSubmit}
      />
      <Button label="Cancel" variant="quiet" disabled={pending} onClick={onCancel} />
    </div>
  )
}
