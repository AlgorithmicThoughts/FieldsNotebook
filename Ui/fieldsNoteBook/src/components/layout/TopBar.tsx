import { ArrowRight, Bell, Building2, ChevronDown, Moon, Sun } from 'lucide-react'
import { IconButton } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'
import type { ResolvedTheme } from '@/hooks/useResolvedTheme'
import './layout.css'

interface TopBarProps {
  projectName: string
  /** Projects.Code, such as OF-017. */
  projectCode: string
  projectPlace: string
  userDisplayName: string
  userInitials: string
  userRoleName: string
  userCrewName: string
  theme: ResolvedTheme
  hasUnreadNotifications: boolean
  onThemeToggle?: () => void
  onProjectChange?: () => void
  onNotificationsOpen?: () => void
}

export function TopBar({
  projectName,
  projectCode,
  projectPlace,
  userDisplayName,
  userInitials,
  userRoleName,
  userCrewName,
  theme,
  hasUnreadNotifications,
  onThemeToggle,
  onProjectChange,
  onNotificationsOpen,
}: TopBarProps) {
  return (
    <header className="topbar">
      <div className="brand">
        <span className="brand-mark">
          <Icon icon={ArrowRight} size={23} />
        </span>
        Field Notebook
      </div>

      <button
        type="button"
        className="project-button"
        onClick={onProjectChange}
        aria-label={`Change project, ${projectName}`}
      >
        <Icon icon={Building2} className="project-icon" />
        <span className="grow">
          <strong>{projectName}</strong>
          <small>
            {projectCode} · {projectPlace}
          </small>
        </span>
        <Icon icon={ChevronDown} />
      </button>

      <div className="spacer" />

      <IconButton
        icon={theme === 'dark' ? Sun : Moon}
        label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
        onClick={onThemeToggle}
      />

      <IconButton
        icon={Bell}
        label={hasUnreadNotifications ? 'Notifications, unread notices' : 'Notifications'}
        onClick={onNotificationsOpen}
        className={hasUnreadNotifications ? 'notifications-unread' : undefined}
      />

      <div className="profile">
        <span className="avatar">{userInitials}</span>
        <span className="profile-copy">
          <strong>{userDisplayName}</strong>
          {userCrewName} · {userRoleName}
        </span>
      </div>
    </header>
  )
}
