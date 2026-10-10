import { Bell } from 'lucide-react'
import type { NotifyPersonOption } from '@/domain/delays'
import { CheckboxRow } from '@/components/common/Checkbox'
import { Icon } from '@/components/common/Icon'

interface NotifyPeopleListProps {
  people: NotifyPersonOption[]
  /** Always a recipient, so the form shows them rather than offering them. */
  responsibleSuperintendent: NotifyPersonOption | null
  value: number[]
  onChange: (userIds: number[]) => void
}

/**
 * Extra recipients are additive. The responsible Superintendent receives the
 * notice whatever the reporter chooses.
 */
export function NotifyPeopleList({
  people,
  responsibleSuperintendent,
  value,
  onChange,
}: NotifyPeopleListProps) {
  function toggle(userId: number, checked: boolean) {
    onChange(checked ? [...value, userId] : value.filter((candidate) => candidate !== userId))
  }

  return (
    <div className="grid gap-2">
      <p className="flex items-center gap-2 rounded-[var(--radius-control)] bg-[var(--soft)] px-3 py-2.5 text-[12px] text-[var(--accent)]">
        <Icon icon={Bell} size={16} />
        {responsibleSuperintendent
          ? `${responsibleSuperintendent.displayName} always receives this notice.`
          : 'The responsible crew’s Superintendent always receives this notice.'}
      </p>
      <div className="grid grid-cols-2 gap-x-4 rounded-[var(--radius-control)] border border-border px-4 py-1">
        {people.map((person) => (
          <CheckboxRow
            key={person.userId}
            checked={value.includes(person.userId)}
            onCheckedChange={(checked) => toggle(person.userId, checked)}
            label={person.displayName}
            detail={`${person.crewName} · ${person.roleName}`}
          />
        ))}
      </div>
    </div>
  )
}
