import type { FormEvent } from 'react'
import type { DelayFormOptions, NotifyPersonOption } from '@/domain/delays'
import { deriveDelayTitle } from '@/domain/delays'
import { CheckboxRow } from '@/components/common/Checkbox'
import { FormField, FormFieldset } from '@/components/common/Field'
import { TextArea, TextInput } from '@/components/common/Input'
import { SelectField, type SelectOption } from '@/components/common/Select'
import { AffectedCrewPicker } from './AffectedCrewPicker'
import { NotifyPeopleList } from './NotifyPeopleList'
import { PhotoPicker } from './PhotoPicker'
import { ReasonTagGroup } from './ReasonTagGroup'
import { SiteMap } from './SiteMap'
import {
  DESCRIPTION_MAX_LENGTH,
  LOCATION_NAME_MAX_LENGTH,
  MAX_PHOTOS,
  type DelayDraft,
  type DelayDraftErrors,
} from './delayDraft'

const SPAN_BOTH = 'col-span-2'

function toOption(item: { id: number; name: string }): SelectOption {
  return { value: String(item.id), label: item.name }
}

function toNumberOrNull(value: string | null): number | null {
  return value === null ? null : Number(value)
}

function superintendentOf(
  options: DelayFormOptions,
  crewId: number | null,
): NotifyPersonOption | null {
  return (
    options.people.find(
      (person) => person.crewId === crewId && person.roleName === 'Superintendent',
    ) ?? null
  )
}

interface DelayFormProps {
  formId: string
  options: DelayFormOptions
  draft: DelayDraft
  errors: DelayDraftErrors
  disabled: boolean
  signedInUserId: number
  onDraftChange: (patch: Partial<DelayDraft>) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}

export function DelayForm({
  formId,
  options,
  draft,
  errors,
  disabled,
  signedInUserId,
  onDraftChange,
  onSubmit,
}: DelayFormProps) {
  const responsibleSuperintendent = superintendentOf(options, draft.ownerCrewId)
  const blockingWorkItems = options.workItems
    .filter((item) => draft.ownerCrewId === null || item.crewIds.includes(draft.ownerCrewId))
    .map((item) => ({ value: String(item.id), label: item.title }))
  const recipients = options.people.filter(
    (person) =>
      person.userId !== signedInUserId && person.userId !== responsibleSuperintendent?.userId,
  )

  return (
    <form id={formId} onSubmit={onSubmit} noValidate className="grid grid-cols-2 gap-[18px]">
      <FormField
        name="description"
        label="What stops the work?"
        required
        error={errors.description}
        description={
          draft.description.trim()
            ? `This delay will be listed as “${deriveDelayTitle(draft.description)}”.`
            : 'The first sentence becomes the delay title.'
        }
        className={SPAN_BOTH}
      >
        <TextArea
          value={draft.description}
          maxLength={DESCRIPTION_MAX_LENGTH}
          disabled={disabled}
          placeholder="Describe the blocker and the work it stops."
          onChange={(event) => onDraftChange({ description: event.target.value })}
        />
      </FormField>

      <FormField name="owner-crew" label="Responsible crew" required error={errors.ownerCrew}>
        <SelectField
          value={draft.ownerCrewId === null ? null : String(draft.ownerCrewId)}
          onValueChange={(value) =>
            onDraftChange({
              ownerCrewId: toNumberOrNull(value),
              // A crew cannot block itself, so drop it from the affected list.
              affectedCrews: draft.affectedCrews.filter(
                (crew) => String(crew.crewId) !== value,
              ),
              blockingWorkItemId: null,
            })
          }
          options={options.crews.map(toOption)}
          placeholder="Must complete the work"
          disabled={disabled}
          required
        />
      </FormField>

      <FormField name="blocking-work-item" label="Blocking work item">
        <SelectField
          value={draft.blockingWorkItemId === null ? null : String(draft.blockingWorkItemId)}
          onValueChange={(value) => onDraftChange({ blockingWorkItemId: toNumberOrNull(value) })}
          options={blockingWorkItems}
          placeholder="Optional"
          disabled={disabled}
        />
      </FormField>

      <FormFieldset
        name="affected-crews"
        legend="Affected crews"
        required
        error={errors.affectedCrews}
        description="Every crew that cannot start or continue because of this blocker."
        className={SPAN_BOTH}
      >
        <AffectedCrewPicker
          crews={options.crews}
          workItems={options.workItems}
          ownerCrewId={draft.ownerCrewId}
          value={draft.affectedCrews}
          onChange={(affectedCrews) => onDraftChange({ affectedCrews })}
        />
      </FormFieldset>

      <FormFieldset
        name="reason"
        legend="Reason"
        required
        error={errors.reason}
        description="The cause of the blocker. This is not a status and not a priority."
        className={SPAN_BOTH}
      >
        <ReasonTagGroup
          tags={options.tags}
          value={draft.delayTagId}
          onValueChange={(delayTagId) => onDraftChange({ delayTagId })}
        />
      </FormFieldset>

      <div className={SPAN_BOTH}>
        <CheckboxRow
          checked={draft.isAdvanceNotice}
          onCheckedChange={(isAdvanceNotice) => onDraftChange({ isAdvanceNotice })}
          disabled={disabled}
          label="Report before the other crew arrives"
          detail="The affected crew's Superintendent receives the tentative completion time."
        />
      </div>

      {draft.isAdvanceNotice ? (
        <FormField
          name="expected-ready-at"
          label="Tentative completion"
          required
          error={errors.expectedReadyAt}
          description="An estimate only. The responsible Superintendent confirms it, and the time alone does not clear the blocker."
          className={SPAN_BOTH}
        >
          <TextInput
            type="datetime-local"
            value={draft.expectedReadyAt}
            disabled={disabled}
            onChange={(event) => onDraftChange({ expectedReadyAt: event.target.value })}
          />
        </FormField>
      ) : null}

      <FormFieldset
        name="photos"
        legend="Photos"
        required
        error={errors.photos}
        className={SPAN_BOTH}
        action={
          <span className="text-[12px] text-muted-foreground">
            {draft.photos.length}/{MAX_PHOTOS}
          </span>
        }
      >
        <PhotoPicker
          photos={draft.photos}
          onChange={(photos) => onDraftChange({ photos })}
          disabled={disabled}
        />
      </FormFieldset>

      <FormField name="segment" label="Segment">
        <SelectField
          value={draft.segmentId === null ? null : String(draft.segmentId)}
          onValueChange={(value) => onDraftChange({ segmentId: toNumberOrNull(value) })}
          options={options.segments.map(toOption)}
          placeholder="Optional"
          disabled={disabled}
        />
      </FormField>

      <FormField
        name="location-name"
        label="Location name"
        description="Required when no photo carries GPS and no pin is placed."
      >
        <TextInput
          value={draft.locationName}
          maxLength={LOCATION_NAME_MAX_LENGTH}
          disabled={disabled}
          placeholder="e.g. Bay 4, Frame 12"
          onChange={(event) => onDraftChange({ locationName: event.target.value })}
        />
      </FormField>

      <FormFieldset
        name="site-map"
        legend="Site map pin"
        error={errors.location}
        description="Select the point on the site image. Photo GPS is shown with each photo and never becomes a pin."
        className={SPAN_BOTH}
      >
        {options.siteMap === null ? (
          <p className="rounded-[12px] border border-dashed border-input bg-[var(--bg)] p-[18px] text-center text-[12px] text-muted-foreground">
            No site image is available for this project. Enter a location name instead.
          </p>
        ) : (
          <SiteMap
            imageName={options.siteMap.name}
            pins={[]}
            editable
            draftPin={draft.pin}
            onPlaceDraftPin={(pin) => onDraftChange({ pin })}
          />
        )}
      </FormFieldset>

      <FormFieldset
        name="notify-people"
        legend="Notify people"
        description="Add anyone else who must see this delay."
        className={SPAN_BOTH}
      >
        <NotifyPeopleList
          people={recipients}
          responsibleSuperintendent={responsibleSuperintendent}
          value={draft.notifyUserIds}
          onChange={(notifyUserIds) => onDraftChange({ notifyUserIds })}
        />
      </FormFieldset>
    </form>
  )
}
