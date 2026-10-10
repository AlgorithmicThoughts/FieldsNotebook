import type { DelaySummary } from './delays'
import type { CurrentUser } from './users'

/**
 * These helpers decide what the screen offers, nothing more. The API is the
 * authority on every delay action, and a client cannot grant permission it
 * does not have. Hiding a control is a courtesy, not a security boundary.
 *
 * Crews are compared by name because a crew name is unique inside a project
 * and both sides of every comparison come from the same active project.
 */

/** Only a Foreman reports a delay. */
export function canReportDelay(user: CurrentUser): boolean {
  return user.roleName === 'Foreman'
}

/** The Superintendent of the owner crew acknowledges the delay. */
export function canAcknowledge(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Superintendent' && user.crewName === delay.ownerCrewName
}

/** A Foreman of an affected crew verifies the handover. */
export function canVerifyHandover(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Foreman' && delay.blockedCrewNames.includes(user.crewName)
}

/** The Superintendent of the owner crew resolves the delay. */
export function canResolve(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Superintendent' && user.crewName === delay.ownerCrewName
}

/** Any Superintendent reopens a delay that is already resolved. */
export function canReopen(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Superintendent' && delay.statusCode === 'Resolved'
}
