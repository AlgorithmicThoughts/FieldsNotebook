import type { CrewOption, WorkItemOption } from '@/domain/delays'
import { CheckboxRow } from '@/components/common/Checkbox'
import { SelectField } from '@/components/common/Select'
import type { AffectedCrewDraft } from './delayDraft'

interface AffectedCrewPickerProps {
  crews: CrewOption[]
  workItems: WorkItemOption[]
  /** The responsible crew cannot also be blocked by its own work. */
  ownerCrewId: number | null
  reporterCrewIds: number[]
  value: AffectedCrewDraft[]
  onChange: (next: AffectedCrewDraft[]) => void
}

/**
 * Every crew the blocker stops, not just one. The approved model permits
 * several, and each one gets its own DelayImpacts row.
 */
export function AffectedCrewPicker({
  crews,
  workItems,
  ownerCrewId,
  reporterCrewIds,
  value,
  onChange,
}: AffectedCrewPickerProps) {
  function toggleCrew(crewId: number, checked: boolean) {
    onChange(
      checked
        ? [...value, { crewId, workItemId: null }]
        : value.filter((crew) => crew.crewId !== crewId),
    )
  }

  function setWorkItem(crewId: number, workItemId: number | null) {
    onChange(value.map((crew) => (crew.crewId === crewId ? { ...crew, workItemId } : crew)))
  }

  return (
    <div className="divide-y divide-border rounded-[var(--radius-control)] border border-border px-4">
      {crews
        .filter((crew) => crew.id !== ownerCrewId)
        .map((crew) => {
          const selected = value.find((candidate) => candidate.crewId === crew.id)
          const options = workItems
            .filter((item) => item.crewIds.includes(crew.id))
            .map((item) => ({ value: String(item.id), label: item.title }))

          return (
            <div key={crew.id} className="py-1">
              <CheckboxRow
                checked={selected !== undefined}
                onCheckedChange={(checked) => toggleCrew(crew.id, checked)}
                label={reporterCrewIds.includes(crew.id) ? `${crew.name} (Your Crew)` : crew.name}
                detail="Cannot start or continue"
              />
              {selected && options.length > 0 ? (
                <div className="pb-3 pl-[31px]">
                  <SelectField
                    label={`Affected work item for ${crew.name}`}
                    value={selected.workItemId === null ? null : String(selected.workItemId)}
                    onValueChange={(next) => setWorkItem(crew.id, next === null ? null : Number(next))}
                    options={options}
                    placeholder="Affected work item (optional)"
                  />
                </div>
              ) : null}
            </div>
          )
        })}
    </div>
  )
}
