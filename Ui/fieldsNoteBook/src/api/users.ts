import type { CurrentUser, RoleName, ThemePreference, UserCrew, UserProject } from '@/domain/users'
import { apiGet, mockRequest } from './client'
import { ApiError, isApiError } from './errors'
import { CREWS, DISTRICTS, PROJECTS, ROLES, USERS, USER_PROJECTS, USER_ROLES, USER_SETTINGS } from './fixtures'
import type { RequestOptions, UserRow, UserSettingsRow } from './types'

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
    .map((project) => ({
      id: project.Id,
      name: project.Name,
      code: project.Code,
      districtName: DISTRICTS.find((district) => district.Id === project.DistrictId)?.Name ?? null,
    }))
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

function crewOf(userId: number, projectId: number): UserCrew {
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

/** The mock settings path has no session, so it answers for the first project. */
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
    crews: [crew],
    activeProjectId: activeProject.id,
    projects,
    ...settings,
  }
}

function userRowOf(userId: number): UserRow {
  const row = USERS.find((candidate) => candidate.Id === userId)

  if (row === undefined) {
    throw new ApiError(404, 'That user is not available in Field Notebook.')
  }

  return row
}

/** GET /api/me */
interface MeResponse {
  id: number
  displayName: string
  email: string
  phone: string | null
  themePreference: ThemePreference
  emailEnabled: boolean
  smsEnabled: boolean
  projects: MeProject[]
}

interface MeProject {
  projectId: number
  name: string
  code: string
  districtName: string
  roleName: RoleName
  crews: UserCrew[]
  canReportDelay: boolean
}

/**
 * Before the user picks a project, the active one is the first where they lead
 * or direct a crew, so Home opens on work they own.
 */
function defaultProjectOf(projects: MeProject[]): MeProject {
  const project = projects.find((candidate) => candidate.crews.length > 0) ?? projects[0]

  if (project === undefined) {
    throw new ApiError(404, 'You are not assigned to a project in Field Notebook.')
  }

  return project
}

/** The project the user picked, which must be one they are assigned to. */
function chosenProjectOf(projects: MeProject[], projectId: number): MeProject {
  const project = projects.find((candidate) => candidate.projectId === projectId)

  if (project === undefined) {
    throw new ApiError(403, 'You are not assigned to that project in Field Notebook.')
  }

  return project
}

function fromMeResponse(me: MeResponse, active: MeProject): CurrentUser {

  return {
    id: me.id,
    displayName: me.displayName,
    initials: initialsOf(me.displayName),
    email: me.email,
    phone: me.phone,
    roleName: active.roleName,
    crews: active.crews,
    activeProjectId: active.projectId,
    projects: me.projects.map((project) => ({
      id: project.projectId,
      name: project.name,
      code: project.code,
      districtName: project.districtName,
    })),
    themePreference: me.themePreference,
    emailEnabled: me.emailEnabled,
    smsEnabled: me.smsEnabled,
  }
}

async function fetchMe(options?: RequestOptions): Promise<MeResponse> {
  try {
    return await apiGet<MeResponse>('/api/me', options)
  } catch (error) {
    // The API answers 404 for an unknown or inactive user.
    if (isApiError(error) && error.status === 404) {
      throw new ApiError(404, 'That user is not available in Field Notebook.')
    }

    throw error
  }
}

/**
 * The signed-in user, with the role they hold on their default project. The
 * API identifies the user from the request, so `userId` only scopes the cache
 * key the caller already uses.
 */
export async function getUser(_userId: number, options?: RequestOptions): Promise<CurrentUser> {
  const me = await fetchMe(options)

  return fromMeResponse(me, defaultProjectOf(me.projects))
}

/**
 * The signed-in user as they stand on another of their assigned projects,
 * since the role and the crews both change with the project. The API keeps no
 * active project, so the choice lives with the caller.
 */
export async function getUserOnProject(
  _userId: number,
  projectId: number,
  options?: RequestOptions,
): Promise<CurrentUser> {
  const me = await fetchMe(options)

  return fromMeResponse(me, chosenProjectOf(me.projects, projectId))
}

/** The four UserSettings columns a user can change. There is no fifth preference. */
export interface UserSettingsUpdate {
  themePreference?: ThemePreference
  emailEnabled?: boolean
  smsEnabled?: boolean
}

/** Writes the fixture row in place, so a later read returns the saved values. */
function writeSettings(userId: number, update: UserSettingsUpdate): void {
  const current = settingsOf(userId)
  const row: UserSettingsRow = {
    UserId: userId,
    ThemePreference: update.themePreference ?? current.themePreference,
    EmailEnabled: update.emailEnabled ?? current.emailEnabled,
    SmsEnabled: update.smsEnabled ?? current.smsEnabled,
  }
  const index = USER_SETTINGS.findIndex((candidate) => candidate.UserId === userId)

  if (index === -1) {
    USER_SETTINGS.push(row)
    return
  }

  USER_SETTINGS[index] = row
}

/** Saves the changed preferences and returns the refreshed user. */
export async function updateUserSettings(
  userId: number,
  settings: UserSettingsUpdate,
  options?: RequestOptions,
): Promise<CurrentUser> {
  return mockRequest(options, () => {
    const row = userRowOf(userId)

    writeSettings(userId, settings)

    return toCurrentUser(row)
  })
}
