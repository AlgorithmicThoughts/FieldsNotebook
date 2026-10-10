import { useOutletContext } from 'react-router-dom'
import type { ProjectSummary } from '@/domain/projects'
import type { CurrentUser } from '@/domain/users'

/** What the layout route loads once and every page reads. */
export interface WorkspaceContext {
  user: CurrentUser
  project: ProjectSummary
}

/** The signed-in user and active project, loaded by the layout route. */
export function useWorkspace(): WorkspaceContext {
  return useOutletContext<WorkspaceContext>()
}
