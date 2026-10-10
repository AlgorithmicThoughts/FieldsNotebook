import { Card, CardHeading } from '@/components/common/Card'
import { crewLabel, type CurrentUser } from '@/domain/users'

interface AccountCardProps {
  user: CurrentUser
  /** The active project, which is the only project this role applies to. */
  projectName: string
}

/** Read-only identity. Nothing here grants a workflow permission. */
export function AccountCard({ user, projectName }: AccountCardProps) {
  return (
    <Card>
      <CardHeading title="Account" />

      <div className="setting-row">
        <div>
          <strong>{user.displayName}</strong>
          <p>{user.email}</p>
        </div>
        <span className="avatar">{user.initials}</span>
      </div>

      <div className="setting-row">
        <div>
          <strong>Phone</strong>
          <p>{user.phone ?? 'Not recorded'}</p>
        </div>
      </div>

      <div className="setting-row">
        <div>
          <strong>Role on {projectName}</strong>
          <p>
            A role is held per project. You can lead a crew as a Foreman on one project and hold a
            different role on another.
          </p>
        </div>
        <span>{user.roleName}</span>
      </div>

      <div className="setting-row">
        <div>
          <strong>Crew</strong>
          <p>{crewLabel(user)}</p>
        </div>
      </div>
    </Card>
  )
}
