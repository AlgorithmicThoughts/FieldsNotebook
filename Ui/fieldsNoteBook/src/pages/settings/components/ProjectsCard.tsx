import { Building2, CircleCheck } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Card, CardHeading } from '@/components/common/Card'
import { Icon } from '@/components/common/Icon'
import type { UserProject } from '@/domain/users'

interface ProjectsCardProps {
  projects: UserProject[]
  activeProjectId: number
}

/** Read-only list. The top bar owns project switching. */
export function ProjectsCard({ projects, activeProjectId }: ProjectsCardProps) {
  return (
    <Card>
      <CardHeading title="Projects" action={<Icon icon={Building2} />} />

      {projects.map((project) => (
        <div className="setting-row" key={project.id}>
          <div>
            <strong>{project.name}</strong>
            <p>{project.code}</p>
          </div>
          {project.id === activeProjectId ? (
            <Badge label="Active" tone="resolved" icon={CircleCheck} />
          ) : null}
        </div>
      ))}

      <p className="setting-note">Change the active project from the top bar.</p>
    </Card>
  )
}
