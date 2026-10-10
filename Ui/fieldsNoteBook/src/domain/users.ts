/** Roles.Name. There are exactly two. */
export type RoleName = 'Foreman' | 'Superintendent'

/** UserSettings.ThemePreference. */
export type ThemePreference = 'System' | 'Light' | 'Dark'

/** One project the user belongs to, from UserProjects joined to Projects. */
export interface UserProject {
  id: number
  name: string
  code: string
  /** Districts.Name, or null when the district row is missing. */
  districtName: string | null
}

/** A crew the user leads as Foreman or directs as Superintendent. */
export interface UserCrew {
  id: number
  name: string
}

/** The signed-in user as the screens need them. */
export interface CurrentUser {
  id: number
  displayName: string
  /** Derived from DisplayName, not stored. */
  initials: string
  email: string
  phone: string | null
  /** The role the user holds in the active project, not across the company. */
  roleName: RoleName
  /**
   * Crews the role covers in the active project: the one crew a Foreman leads,
   * or every crew a Superintendent directs. Empty when the role covers none.
   */
  crews: UserCrew[]
  activeProjectId: number
  projects: UserProject[]
  themePreference: ThemePreference
  emailEnabled: boolean
  smsEnabled: boolean
}

/**
 * Crews are compared by name because a crew name is unique inside a project
 * and both sides of every comparison come from the same active project.
 */
export function coversCrew(user: CurrentUser, crewName: string): boolean {
  return user.crews.some((crew) => crew.name === crewName)
}

export function coversCrewId(user: CurrentUser, crewId: number): boolean {
  return user.crews.some((crew) => crew.id === crewId)
}

/** "Welding and Erection", or every directed crew for a Superintendent. */
export function crewLabel(user: CurrentUser): string {
  return user.crews.length ? user.crews.map((crew) => crew.name).join(', ') : 'No crew assigned'
}
