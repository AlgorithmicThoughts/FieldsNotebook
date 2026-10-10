import type { CurrentUser, RoleName, ThemePreference, UserProject } from '@/domain/users'
import { mockRequest } from './client'
import { ApiError } from './errors'
import { CREWS, PROJECTS, ROLES, USERS, USER_PROJECTS, USER_ROLES, USER_SETTINGS } from './fixtures'
import type { RequestOptions, UserRow } from './types'

interface UserSettingsValues {
  themePreference: ThemePreference
  emailEnabled: boolean
  smsEnabled: boolean
}

const DEFAULT_SETTINGS: UserSettingsValues = {
  themePreference: 'System',
  emailEnabled: true,
  smsEnabled: false,
}

function initialsOf(displayName: string): string {
  const parts = displayName.trim().split(/\s+/)
  const first = parts[0]?.charAt(0) ?? ''
  const last = parts.length > 1 ? (parts.at(-1)?.charAt(0) ?? '') : ''

  return (first + last).toUpperCase()
}

function projectsOf(userId: number): UserProject[] {
  return USER_PROJECTS.filter((row) => row.UserId === userId && row.IsActive)
    .map((row) => PROJECTS.find((project) => project.Id === row.ProjectId))
    .filter((project) => project !== undefined)
    .map((project) => ({ id: project.Id, name: project.Name, code: project.Code }))
}

/**
 * A user holds a different role in different projects, so the role is only
 * meaningful alongside the active project.
 */
function roleNameOf(userId: number, projectId: number): RoleName {
  const userRole = USER_ROLES.find(
    (row) => row.UserId === userId && row.ProjectId === projectId && row.IsActive,
  )
  const role = userRole && ROLES.find((candidate) => candidate.Id === userRole.RoleId)

  if (!role) {
    throw new ApiError(500, 'Field Notebook could not read your role on this project.')
  }

  return role.Name
}

function crewOf(userId: number, projectId: number): { id: number; name: string } {
  const crew = CREWS.find(
    (candidate) =>
      candidate.ProjectId === projectId &&
      (candidate.ForemanLeadId === userId || candidate.SuperintendentUserId === userId),
  )

  if (crew === undefined) {
    throw new ApiError(500, 'Field Notebook could not read your crew on this project.')
  }

  return { id: crew.Id, name: crew.Name }
}

function settingsOf(userId: number): UserSettingsValues {
  const settings = USER_SETTINGS.find((row) => row.UserId === userId)

  if (settings === undefined) {
    return DEFAULT_SETTINGS
  }

  return {
    themePreference: settings.ThemePreference,
    emailEnabled: settings.EmailEnabled,
    smsEnabled: settings.SmsEnabled,
  }
}

/**
 * The mock treats the user's first project as the active one. The real API
 * returns the active project, so only this line changes with it.
 */
function activeProjectOf(projects: UserProject[]): UserProject {
  const project = projects[0]

  if (project === undefined) {
    throw new ApiError(404, 'You are not assigned to a project in Field Notebook.')
  }

  return project
}

function toCurrentUser(row: UserRow): CurrentUser {
  const projects = projectsOf(row.Id)
  const activeProject = activeProjectOf(projects)
  const crew = crewOf(row.Id, activeProject.id)
  const settings = settingsOf(row.Id)

  return {
    id: row.Id,
    displayName: row.DisplayName,
    initials: initialsOf(row.DisplayName),
    email: row.Email,
    phone: row.Phone,
    roleName: roleNameOf(row.Id, activeProject.id),
    crewId: crew.id,
    crewName: crew.name,
    activeProjectId: activeProject.id,
    projects,
    ...settings,
  }
}

/** The signed-in user, with the role they hold on their active project. */
export async function getUser(userId: number, options?: RequestOptions): Promise<CurrentUser> {
  return mockRequest(options, () => {
    const row = USERS.find((candidate) => candidate.Id === userId)

    if (row === undefined) {
      throw new ApiError(404, 'That user is not available in Field Notebook.')
    }

    return toCurrentUser(row)
  })
}
