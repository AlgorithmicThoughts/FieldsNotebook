/** Roles.Name. There are exactly two. */
export type RoleName = 'Foreman' | 'Superintendent'

/** UserSettings.ThemePreference. */
export type ThemePreference = 'System' | 'Light' | 'Dark'

/** One project the user belongs to, from UserProjects joined to Projects. */
export interface UserProject {
  id: number
  name: string
  code: string
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
  /** Crew the user leads or supervises in the active project. */
  crewId: number
  crewName: string
  activeProjectId: number
  projects: UserProject[]
  themePreference: ThemePreference
  emailEnabled: boolean
  smsEnabled: boolean
}
