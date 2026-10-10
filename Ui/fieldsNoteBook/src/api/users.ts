import type { CurrentUser, RoleName, ThemePreference, UserCrew } from '@/domain/users'
import { apiGet, apiPatch } from './client'
import { ApiError, isApiError } from './errors'
import type { RequestOptions } from './types'

function initialsOf(displayName: string): string {
  const parts = displayName.trim().split(/\s+/)
  const first = parts[0]?.charAt(0) ?? ''
  const last = parts.length > 1 ? (parts.at(-1)?.charAt(0) ?? '') : ''

  return (first + last).toUpperCase()
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

/** PATCH /api/me/settings answers with the saved row. */
export interface UserSettings {
  themePreference: ThemePreference
  emailEnabled: boolean
  smsEnabled: boolean
}

/**
 * Saves only the preferences in `settings`, so a theme save and a channel save
 * in flight together cannot undo each other. The API identifies the user from
 * the request, so `userId` is not sent.
 */
export async function updateUserSettings(
  _userId: number,
  settings: UserSettingsUpdate,
  options?: RequestOptions,
): Promise<UserSettings> {
  return apiPatch<UserSettings>('/api/me/settings', settings, options)
}
