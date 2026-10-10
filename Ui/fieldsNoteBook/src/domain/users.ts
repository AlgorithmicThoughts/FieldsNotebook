import type { CurrentUserProject } from '@/api/currentUser'

/** Up to two initials, such as "JM" for John Miller. */
export function toInitials(displayName: string): string {
  const words = displayName.trim().split(/\s+/).filter(Boolean)
  const letters = words.length > 1 ? [words[0], words[words.length - 1]] : words
  return letters.map((word) => word[0].toUpperCase()).join('')
}

/** The crews this role covers in the project, or a plain note when it covers none. */
export function toCrewLabel(project: CurrentUserProject): string {
  return project.crewNames.length ? project.crewNames.join(', ') : 'No crew assigned'
}

/** Prefer a project where the user leads or directs a crew; otherwise the first assignment. */
export function pickDefaultProject(projects: CurrentUserProject[]): CurrentUserProject | undefined {
  return projects.find((project) => project.crewNames.length > 0) ?? projects[0]
}
