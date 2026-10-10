import { Bell, Moon, Sun } from 'lucide-react'
import { IconButton } from '@/components/common/Button'
import type { ResolvedTheme } from '@/hooks/useResolvedTheme'
import type { UserProject } from '@/domain/users'
import { Brand } from './Brand'
import { ProfileMenu } from './ProfileMenu'
import { ProjectMenu } from './ProjectMenu'
import './layout.css'

interface TopBarProps {
  projectName: string
  /** Projects.Code, such as OMA-FAB. */
  projectCode: string
  projectPlace: string
  /** Every project the user is assigned to, so the menu never reaches past them. */
  projects: UserProject[]
  activeProjectId: number
  isSwitchingProject: boolean
  userDisplayName: string
  userInitials: string
  userRoleName: string
  userCrewName: string
  theme: ResolvedTheme
  hasUnreadNotifications: boolean
  onThemeToggle?: () => void
  onProjectChange?: (projectId: number) => void
  onNotificationsOpen?: () => void
  onSignOut: () => void
}

export function TopBar({
  projectName,
  projectCode,
  projectPlace,
  projects,
  activeProjectId,
  isSwitchingProject,
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

      <ProjectMenu
        projects={projects}
        activeProjectId={activeProjectId}
        activeProjectName={projectName}
        activeProjectCode={projectCode}
        activeProjectPlace={projectPlace}
        isSwitching={isSwitchingProject}
        onSelectProject={onProjectChange}
      />

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
