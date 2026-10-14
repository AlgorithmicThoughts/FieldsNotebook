import { useEffect, useRef, useState } from 'react'
import { Building2, Check, ChevronDown } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import type { UserProject } from '@/domain/users'
import './layout.css'

interface ProjectMenuProps {
  projects: UserProject[]
  activeProjectId: number
  activeProjectName: string
  /** Projects.Code, such as OMA-FAB. */
  activeProjectCode: string
  activeProjectPlace: string
  /** True while the chosen project loads, so a second pick cannot race it. */
  isSwitching: boolean
  onSelectProject?: (projectId: number) => void
}

/**
 * The active project in the top bar, and the assigned projects it switches
 * between. A user never sees a project they are not assigned to.
 */
export function ProjectMenu({
  projects,
  activeProjectId,
  activeProjectName,
  activeProjectCode,
  activeProjectPlace,
  isSwitching,
  onSelectProject,
}: ProjectMenuProps) {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  // The menu belongs to the document while it is open: a click anywhere else,
  // or Escape, closes it.
  useEffect(() => {
    if (!isOpen) {
      return
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) {
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

  function selectProject(projectId: number) {
    setIsOpen(false)

    if (projectId !== activeProjectId) {
      onSelectProject?.(projectId)
    }
  }

  return (
    <div className="project-menu" ref={menuRef}>
      <button
        type="button"
        className="project-button"
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-label={`Change project, ${activeProjectName}`}
        disabled={isSwitching}
        onClick={() => setIsOpen((open) => !open)}
      >
        <Icon icon={Building2} className="project-icon" />
        <span className="grow">
          <strong>{activeProjectName}</strong>
          <small>
            {isSwitching ? 'Switching project…' : `${activeProjectCode} · ${activeProjectPlace}`}
          </small>
        </span>
        <Icon icon={ChevronDown} />
      </button>

      {isOpen ? (
        <div className="project-popover" role="menu">
          {projects.map((project) => (
            <button
              key={project.id}
              type="button"
              role="menuitemradio"
              aria-checked={project.id === activeProjectId}
              className="project-option"
              onClick={() => selectProject(project.id)}
            >
              <span className="grow">
                <strong>{project.name}</strong>
                <small>{project.code}</small>
              </span>
              {project.id === activeProjectId ? <Icon icon={Check} size={16} /> : null}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  )
}
