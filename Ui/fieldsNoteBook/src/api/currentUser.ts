/** GET /api/me */
export interface CurrentUser {
  id: number
  displayName: string
  projects: CurrentUserProject[]
}

export interface CurrentUserProject {
  projectId: number
  name: string
  /** Projects.Code, such as OMA-FAB. */
  code: string
  districtName: string
  roleName: string
  /** Crews the user leads as Foreman, or directs as Superintendent, in this project. */
  crewNames: string[]
  canReportDelay: boolean
}

export const CURRENT_USER_PATH = '/api/me'
