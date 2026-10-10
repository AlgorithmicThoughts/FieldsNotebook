import { RefreshCw, Search } from 'lucide-react'
import { Button } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'
import { TextInput } from '@/components/common/Input'
import { SelectField, type SelectOption } from '@/components/common/Select'
import type { TrackingScope } from '../trackingView'

/** Only the projects on `user.projects` are ever requested. */
const SCOPE_OPTIONS: SelectOption[] = [
  { value: 'project', label: 'This project' },
  { value: 'all', label: 'All assigned projects' },
]

interface TrackingToolbarProps {
  scope: TrackingScope
  search: string
  isLoading: boolean
  onScopeChange: (scope: TrackingScope) => void
  onSearchChange: (search: string) => void
  onRefresh: () => void
}

export function TrackingToolbar({
  scope,
  search,
  isLoading,
  onScopeChange,
  onSearchChange,
  onRefresh,
}: TrackingToolbarProps) {
  return (
    <div className="tracking-toolbar">
      <div className="tracking-scope">
        <SelectField
          label="Delays to show"
          value={scope}
          onValueChange={(value) => onScopeChange(value === 'all' ? 'all' : 'project')}
          options={SCOPE_OPTIONS}
          placeholder="This project"
        />
      </div>

      <div className="tracking-search">
        <Icon icon={Search} />
        <TextInput
          type="search"
          value={search}
          aria-label="Search delays"
          placeholder="Search a delay, a number, or a reason"
          className="pl-[42px]!"
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </div>

      <Button label="Refresh" icon={RefreshCw} disabled={isLoading} onClick={onRefresh} />
    </div>
  )
}
