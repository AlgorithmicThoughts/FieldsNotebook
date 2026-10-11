import type { FormEvent } from 'react'
import type { DelayFormOptions, NotifyPersonOption } from '@/domain/delays'
import { CheckboxRow } from '@/components/common/Checkbox'
import { FormField, FormFieldset } from '@/components/common/Field'
import { TextArea, TextInput } from '@/components/common/Input'
import { SelectField, type SelectOption } from '@/components/common/Select'
import { AffectedCrewPicker } from './AffectedCrewPicker'
import { NotifyPeopleList } from './NotifyPeopleList'
import { PhotoPicker } from './PhotoPicker'
import { ReasonTagGroup } from './ReasonTagGroup'
import { SiteMap } from './SiteMap'
import { DelayRecordingControls } from './DelayRecordingControls'
import {
  DESCRIPTION_MAX_LENGTH,
  LOCATION_NAME_MAX_LENGTH,
  MAX_PHOTOS,
  TITLE_MAX_LENGTH,
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

function selectionOf(value: number | null): string | null {
  return value === null ? null : String(value)
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
  reporterCrewIds: number[]
  onPhotoReadingChange: (reading: boolean) => void
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
  reporterCrewIds,
  onPhotoReadingChange,
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
  ).filter((person, index, people) => people.findIndex((p) => p.userId === person.userId) === index)

  return (
    <form id={formId} onSubmit={onSubmit} noValidate className="grid grid-cols-2 gap-[18px]">
      <fieldset disabled={disabled} className="contents">
      <FormField name="delay-title" label="Delay title" required error={errors.title}
        className={SPAN_BOTH} action={<DelayRecordingControls disabled={disabled} />}>
        <TextInput autoFocus value={draft.title} maxLength={TITLE_MAX_LENGTH} disabled={disabled}
          onChange={(event) => onDraftChange({ title: event.target.value })} />
      </FormField>
      <FormField
        name="description"
        label="What stops the work?"
        required
        error={errors.description}
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
          value={selectionOf(draft.ownerCrewId)}
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
          value={selectionOf(draft.blockingWorkItemId)}
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
          reporterCrewIds={reporterCrewIds}
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
          label="Ask for a tentative resolution time"
          detail="The responsible crew's Superintendent adds a tentative completion time when acknowledging the delay."
        />
      </div>

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
          onReadingChange={onPhotoReadingChange}
        />
      </FormFieldset>

      <FormField name="segment" label="Segment" required error={errors.segment}>
        <SelectField
          value={selectionOf(draft.segmentId)}
          onValueChange={(value) => onDraftChange({ segmentId: toNumberOrNull(value) })}
          options={options.segments.map(toOption)}
          placeholder="Choose a segment"
          required
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
        <SiteMap
          imageName={options.siteMap?.name ?? 'Site map'}
          pins={[]}
          editable={!disabled}
          draftPin={draft.pin}
          onPlaceDraftPin={(pin) => onDraftChange({ pin })}
        />
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
      </fieldset>
    </form>
  )
}
