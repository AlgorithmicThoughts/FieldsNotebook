import type { DelaySummary } from './delays'
import { coversCrew, type CurrentUser } from './users'

/**
 * These helpers decide what the screen offers, nothing more. The API is the
 * authority on every delay action, and a client cannot grant permission it
 * does not have. Hiding a control is a courtesy, not a security boundary.
 *
 * Crew comparisons use `coversCrew`, so a Superintendent who directs several
 * crews is checked against all of them.
 */

/** Only a Foreman who leads a crew in the project reports a delay. */
export function canReportDelay(user: CurrentUser): boolean {
  return user.roleName === 'Foreman' && user.crews.length > 0
}

/** The Superintendent who directs the crew that must remove the blocker. */
function leadsOwnerCrew(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Superintendent' && coversCrew(user, delay.ownerCrewName)
}

/** The Superintendent of the owner crew acknowledges the delay. */
export function canAcknowledge(user: CurrentUser, delay: DelaySummary): boolean {
  return leadsOwnerCrew(user, delay)
}

/** The responsible Superintendent gives or revises the tentative completion time. */
export function canSetCompletionTime(user: CurrentUser, delay: DelaySummary): boolean {
  return leadsOwnerCrew(user, delay)
}

/** The responsible Superintendent asks the affected crew for a handover check. */
export function canRequestHandoverCheck(user: CurrentUser, delay: DelaySummary): boolean {
  return leadsOwnerCrew(user, delay)
}

/** A Foreman of an affected crew verifies the handover. */
export function canVerifyHandover(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Foreman' && delay.blockedCrewNames.some((name) => coversCrew(user, name))
}

/** The Superintendent of the owner crew resolves the delay. */
export function canResolve(user: CurrentUser, delay: DelaySummary): boolean {
  return leadsOwnerCrew(user, delay)
}

/**
 * The Superintendent of the Foreman who raised the delay withdraws it. The
 * reporter's crew comes from the delay detail, not from the summary row.
 */
export function canRevoke(user: CurrentUser, reportedByCrewName: string): boolean {
  return user.roleName === 'Superintendent' && coversCrew(user, reportedByCrewName)
}

/** Any Superintendent reopens a delay that is already resolved. */
export function canReopen(user: CurrentUser, delay: DelaySummary): boolean {
  return user.roleName === 'Superintendent' && delay.statusCode === 'Resolved'
}
