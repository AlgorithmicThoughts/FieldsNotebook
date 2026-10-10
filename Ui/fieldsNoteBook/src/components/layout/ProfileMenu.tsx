import { useEffect, useRef, useState } from 'react'
import { ChevronDown } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import './layout.css'

interface ProfileMenuProps {
  userDisplayName: string
  userInitials: string
  userRoleName: string
  userCrewName: string
  onSignOut: () => void
}

/** The profile in the top bar, and the sign-out action it opens. */
export function ProfileMenu({
  userDisplayName,
  userInitials,
  userRoleName,
  userCrewName,
  onSignOut,
}: ProfileMenuProps) {
  const [isOpen, setIsOpen] = useState(false)
  const menu = useRef<HTMLDivElement>(null)

  // The menu belongs to the document while it is open: a click anywhere else,
  // or Escape, closes it.
  useEffect(() => {
    if (!isOpen) {
      return
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!menu.current?.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }

    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [isOpen])

  return (
    <div className="profile-menu" ref={menu}>
      <button
        type="button"
        className="profile"
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-label={`Account menu, ${userDisplayName}`}
        onClick={() => setIsOpen((open) => !open)}
      >
        <span className="avatar">{userInitials}</span>
        <span className="profile-copy">
          <strong>{userDisplayName}</strong>
          {userCrewName} · {userRoleName}
        </span>
        <Icon icon={ChevronDown} />
      </button>

      {isOpen ? (
        <div className="profile-popover" role="menu">
          <button type="button" role="menuitem" className="profile-action" onClick={onSignOut}>
            Sign out
          </button>
        </div>
      ) : null}
    </div>
  )
}
