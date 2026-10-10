import { Bell, Building2, ChevronDown, Moon, Sun } from 'lucide-react'
import { IconButton } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'
import type { ResolvedTheme } from '@/hooks/useResolvedTheme'
import { Brand } from './Brand'
import { ProfileMenu } from './ProfileMenu'
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
  onSignOut: () => void
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
  onSignOut,
}: TopBarProps) {
  return (
    <header className="topbar">
      <Brand />

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

      <ProfileMenu
        userDisplayName={userDisplayName}
        userInitials={userInitials}
        userRoleName={userRoleName}
        userCrewName={userCrewName}
        onSignOut={onSignOut}
      />
    </header>
  )
}
