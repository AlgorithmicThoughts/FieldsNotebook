import { Users, type LucideIcon } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { Icon } from '@/components/common/Icon'
import './layout.css'

export interface NavItem {
  id: string
  /** Route path the item navigates to. */
  path: string
  label: string
  icon: LucideIcon
  /** Unread or outstanding count beside the label. */
  count?: number
}

interface SidebarProps {
  items: NavItem[]
  crewName: string
  roleName: string
  projectTimeLabel: string
}

export function Sidebar({ items, crewName, roleName, projectTimeLabel }: SidebarProps) {
  return (
    <aside className="sidebar">
      <div className="eyebrow sidebar-eyebrow">Workspace</div>

      <nav aria-label="Primary">
        {items.map((item) => (
          <NavLink
            key={item.id}
            to={item.path}
            end={item.path === '/'}
            className={({ isActive }) => (isActive ? 'nav-button active' : 'nav-button')}
          >
            <Icon icon={item.icon} />
            <span className="grow">{item.label}</span>
            {item.count ? <span className="nav-count">{item.count}</span> : null}
          </NavLink>
        ))}
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
