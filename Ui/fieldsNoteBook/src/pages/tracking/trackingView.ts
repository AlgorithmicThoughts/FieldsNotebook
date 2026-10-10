import {
  CalendarDays,
  CircleCheck,
  ClipboardCheck,
  RotateCcw,
  Send,
  Wrench,
  X,
  type LucideIcon,
} from 'lucide-react'
import { isDelayActive, type DelayDetail, type DelaySummary } from '@/domain/delays'
import {
  canAcknowledge,
  canReopen,
  canRequestHandoverCheck,
  canResolve,
  canRevoke,
  canSetCompletionTime,
  canVerifyHandover,
} from '@/domain/permissions'
import { coversCrew, type CurrentUser } from '@/domain/users'

/** The delays the toolbar asks for: the active project, or every assigned one. */
export type TrackingScope = 'project' | 'all'

export type TrackingFilter = 'all' | 'blocking' | 'waiting' | 'verify' | 'closed'

export const TRACKING_FILTERS: { value: TrackingFilter; label: string }[] = [
  { value: 'all', label: 'All delays' },
  { value: 'blocking', label: 'Blocking us' },
  { value: 'waiting', label: 'Waiting on us' },
  { value: 'verify', label: 'Ready to verify' },
  { value: 'closed', label: 'Closed' },
]

/**
 * The role and crew on `CurrentUser` belong to the active project, so a crew
 * comparison is only meaningful there. A delay from another assigned project
 * still lists, but it never counts as our crew's work.
 */
function isActiveProject(delay: DelaySummary, user: CurrentUser): boolean {
  return delay.projectId === user.activeProjectId
}

function affectsOurCrew(delay: DelaySummary, user: CurrentUser): boolean {
  return isActiveProject(delay, user) && delay.blockedCrewNames.some((name) => coversCrew(user, name))
}

function ownedByOurCrew(delay: DelaySummary, user: CurrentUser): boolean {
  return isActiveProject(delay, user) && coversCrew(user, delay.ownerCrewName)
}

const FILTER_RULES: Record<TrackingFilter, (delay: DelaySummary, user: CurrentUser) => boolean> = {
  all: () => true,
  blocking: (delay, user) => isDelayActive(delay) && affectsOurCrew(delay, user),
  waiting: (delay, user) => isDelayActive(delay) && ownedByOurCrew(delay, user),
  // Only the affected crew's Foreman verifies that crew's handover.
  verify: (delay, user) =>
    delay.statusCode === 'ReadyForHandover' &&
    isActiveProject(delay, user) &&
    canVerifyHandover(user, delay),
  closed: (delay) => !isDelayActive(delay),
}

/** Search matches the title, the FN-#### number, and the reason tag. */
function matchesSearch(delay: DelaySummary, search: string): boolean {
  if (search === '') {
    return true
  }

  return [delay.title, delay.number, delay.tagName].some((value) =>
    value.toLowerCase().includes(search),
  )
}

/** A reopened or High priority delay is the most pressing row in the list. */
function isPressing(delay: DelaySummary): boolean {
  return delay.isHighPriority || delay.statusCode === 'Reopened'
}

function byPressingThenNewest(left: DelaySummary, right: DelaySummary): number {
  const byPriority = Number(isPressing(right)) - Number(isPressing(left))

  return byPriority !== 0 ? byPriority : Date.parse(right.createdAt) - Date.parse(left.createdAt)
}

/**
 * The rows one tab and one search term show, derived from the loaded delays on
 * every render rather than kept as a second list in state.
 */
export function toVisibleDelays(
  delays: DelaySummary[],
  user: CurrentUser,
  filter: TrackingFilter,
  search: string,
): DelaySummary[] {
  const term = search.trim().toLowerCase()

  return delays
    .filter((delay) => FILTER_RULES[filter](delay, user) && matchesSearch(delay, term))
    .sort(byPressingThenNewest)
}

/**
 * `key` records the request the loaded delays belong to, so a late answer for
 * a previous scope or a previous attempt never renders.
 */
export type TrackingState =
  | { key: null }
  | { key: string; status: 'error'; message: string }
  | { key: string; status: 'ready'; delays: DelaySummary[]; emptyMessage: string }

export type TrackingResultsView =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; delays: DelaySummary[]; emptyMessage?: string }

/** What the results card shows for the current request, tab, and search term. */
export function toResultsView(
  state: TrackingState,
  requestKey: string,
  user: CurrentUser,
  filter: TrackingFilter,
  search: string,
): TrackingResultsView {
  if (state.key === null || state.key !== requestKey) {
    return { kind: 'loading' }
  }

  if (state.status === 'error') {
    return { kind: 'error', message: state.message }
  }

  return {
    kind: 'ready',
    delays: toVisibleDelays(state.delays, user, filter, search),
    emptyMessage: state.delays.length === 0 ? state.emptyMessage : undefined,
  }
}

/** The delays of the current request, whatever the tab and search term hide. */
export function toLoadedDelays(state: TrackingState, requestKey: string): DelaySummary[] {
  if (state.key === null || state.key !== requestKey || state.status === 'error') {
    return []
  }

  return state.delays
}

/** One workflow action the screen offers the current user. */
export interface DelayActionOffer {
  id: string
  label: string
  icon: LucideIcon
}

interface ActionRule extends DelayActionOffer {
  isOffered: (delay: DelayDetail, user: CurrentUser) => boolean
}

/** Every affected crew verified the handover for the current cycle. */
function hasEveryHandoverCheck(delay: DelayDetail): boolean {
  return (
    delay.impacts.length > 0 &&
    delay.impacts.every((impact) => impact.verifications.length > 0)
  )
}

const ACTION_RULES: ActionRule[] = [
  {
    id: 'acknowledge',
    label: 'Acknowledge',
    icon: Wrench,
    isOffered: (delay, user) =>
      canAcknowledge(user, delay) &&
      (delay.statusCode === 'Raised' || delay.statusCode === 'Reopened'),
  },
  {
    id: 'completionTime',
    label: 'Revise completion time',
    icon: CalendarDays,
    isOffered: (delay, user) => canSetCompletionTime(user, delay) && isDelayActive(delay),
  },
  {
    id: 'requestCheck',
    label: 'Request handover check',
    icon: Send,
    isOffered: (delay, user) =>
      canRequestHandoverCheck(user, delay) && delay.statusCode === 'Acknowledged',
  },
  {
    id: 'verify',
    label: 'Verify handover',
    icon: ClipboardCheck,
    isOffered: (delay, user) =>
      canVerifyHandover(user, delay) && delay.statusCode === 'ReadyForHandover',
  },
  {
    // A resolve waits for the required checks, never for the estimate alone.
    id: 'resolve',
    label: 'Resolve',
    icon: CircleCheck,
    isOffered: (delay, user) =>
      canResolve(user, delay) &&
      delay.statusCode === 'ReadyForHandover' &&
      hasEveryHandoverCheck(delay),
  },
  {
    id: 'reopen',
    label: 'Reopen',
    icon: RotateCcw,
    isOffered: (delay, user) => canReopen(user, delay),
  },
  {
    id: 'revoke',
    label: 'Withdraw',
    icon: X,
    isOffered: (delay, user) =>
      canRevoke(user, delay.reportedBy.crewName) && isDelayActive(delay),
  },
]

/**
 * The actions this user is entitled to on this delay. The API is the authority
 * on every one of them, and none of them has a write endpoint yet, so the
 * detail view renders them disabled.
 */
export function toOfferedActions(delay: DelayDetail, user: CurrentUser): DelayActionOffer[] {
  if (delay.projectId !== user.activeProjectId) {
    return []
  }

  return ACTION_RULES.filter((rule) => rule.isOffered(delay, user)).map(
    ({ id, label, icon }) => ({ id, label, icon }),
  )
}

/** One stage of the handover, with the crew and role it waits on. */
export interface HandoverStep {
  id: string
  label: string
  actor: string
  state: 'done' | 'current' | 'pending'
}

function affectedCrewLabel(delay: DelayDetail): string {
  return delay.blockedCrewNames.length === 0
    ? 'the affected crew'
    : delay.blockedCrewNames.join(', ')
}

/**
 * The stage the current cycle sits in. ReadyForHandover covers both the wait
 * for the checks and the wait for the resolve, so the checks separate them.
 * A reopened cycle starts the stages again, which is why it reads as the first.
 */
function currentStageIndex(delay: DelayDetail): number {
  switch (delay.statusCode) {
    case 'Acknowledged':
      return 1
    case 'ReadyForHandover':
      return hasEveryHandoverCheck(delay) ? 3 : 2
    case 'Resolved':
      return 4
    default:
      return 0
  }
}

/** The five handover stages of the current cycle, earliest first. */
export function toHandoverSteps(delay: DelayDetail): HandoverStep[] {
  const ownerActor = `${delay.ownerCrewName} · Superintendent`
  const stages = [
    {
      id: 'raised',
      label: 'Delay raised',
      actor: `${delay.reportedBy.crewName} · ${delay.reportedBy.displayName}`,
    },
    { id: 'acknowledge', label: 'Acknowledge', actor: ownerActor },
    { id: 'ready', label: 'Ready for handover', actor: ownerActor },
    {
      id: 'verify',
      label: 'Verify handover',
      actor: `${affectedCrewLabel(delay)} · Foreman`,
    },
    { id: 'resolve', label: 'Resolve delay', actor: ownerActor },
  ]
  const current = currentStageIndex(delay)
  const isResolved = delay.statusCode === 'Resolved'

  return stages.map((stage, index) => ({
    ...stage,
    state: isResolved || index < current ? 'done' : index === current ? 'current' : 'pending',
  }))
}

/**
 * What this user does next on this delay. The sentence follows the same
 * permission helpers the offered actions use, so the two never disagree.
 */
export function toNextStepNote(delay: DelayDetail, user: CurrentUser): string {
  if (delay.projectId !== user.activeProjectId) {
    return `This delay belongs to ${delay.projectName}. Switch to that project to act on it.`
  }

  if (delay.statusCode === 'Revoked') {
    return 'The reporting Foreman’s Superintendent withdrew this delay. Nothing is outstanding.'
  }

  if (delay.statusCode === 'Resolved') {
    return canReopen(user, delay)
      ? 'This delay is resolved. Reopen it if the blocker is still on site.'
      : 'This delay is resolved. Any Superintendent can reopen it if the blocker remains.'
  }

  if (
    (delay.statusCode === 'Raised' || delay.statusCode === 'Reopened') &&
    canAcknowledge(user, delay)
  ) {
    return 'Speak to your Foreman, then acknowledge this delay.'
  }

  if (delay.statusCode === 'Acknowledged' && canRequestHandoverCheck(user, delay)) {
    return `Ask the Foreman of ${affectedCrewLabel(delay)} for a handover check once the work is ready.`
  }

  if (delay.statusCode === 'ReadyForHandover' && canVerifyHandover(user, delay)) {
    return 'Check the finished work on site, then verify the handover for your crew.'
  }

  if (delay.statusCode === 'ReadyForHandover' && canResolve(user, delay)) {
    return hasEveryHandoverCheck(delay)
      ? 'Every affected crew verified the handover, so you can resolve this delay.'
      : 'Wait for every affected crew’s Foreman to verify the handover before you resolve it.'
  }

  return `Nothing waits on you. ${delay.ownerCrewName} must remove the blocker.`
}
