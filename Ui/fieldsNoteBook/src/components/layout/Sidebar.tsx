import { Users, type LucideIcon } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import './layout.css'

export interface NavItem {
  id: string
  label: string
  icon: LucideIcon
  /** Unread or outstanding count beside the label. */
  count?: number
}

interface SidebarProps {
  items: NavItem[]
  activeItemId: string
  crewName: string
  roleName: string
  projectTimeLabel: string
  onSelect?: (itemId: string) => void
}

export function Sidebar({
  items,
  activeItemId,
  crewName,
  roleName,
  projectTimeLabel,
  onSelect,
}: SidebarProps) {
  return (
    <aside className="sidebar">
      <div className="eyebrow sidebar-eyebrow">Workspace</div>

      <nav aria-label="Primary">
        {items.map((item) => {
          const isActive = item.id === activeItemId

          return (
            <button
              key={item.id}
              type="button"
              className={isActive ? 'nav-button active' : 'nav-button'}
              aria-current={isActive ? 'page' : undefined}
              onClick={() => onSelect?.(item.id)}
            >
              <Icon icon={item.icon} />
              <span className="grow">{item.label}</span>
              {item.count ? <span className="nav-count">{item.count}</span> : null}
            </button>
          )
        })}
      </nav>

      <div className="side-note">
        <Icon icon={Users} />
        <strong>{crewName}</strong>
        {roleName}
        <br />
        {projectTimeLabel}
      </div>
    </aside>
  )
}
