import {
  CalendarDays,
  CircleCheck,
  RotateCcw,
  Send,
  Undo2,
  Wrench,
  X,
  type LucideIcon,
} from "lucide-react";
import type { ApiQueryState } from "@/hooks/useApiQuery";
import {
  isDelayActive,
  type DelayDetail,
  type DelaySummary,
} from "@/domain/delays";
import {
  canAcknowledge,
  canReopen,
  canRequestHandoverCheck,
  canRevoke,
  canSetCompletionTime,
  canVerifyHandover,
} from "@/domain/permissions";
import { coversCrew, type CurrentUser } from "@/domain/users";
import { verifiableImpacts } from "./delayActionDraft";

/** The delays the toolbar asks for: the active project, or every assigned one. */
export type TrackingScope = "project" | "all";

export type TrackingFilter =
  | "all"
  | "blocking"
  | "waiting"
  | "verify"
  | "closed";

export const TRACKING_FILTERS: { value: TrackingFilter; label: string }[] = [
  { value: "all", label: "All delays" },
  { value: "blocking", label: "Blocking us" },
  { value: "waiting", label: "Waiting on us" },
  { value: "verify", label: "Ready to review" },
  { value: "closed", label: "Closed" },
];

/**
 * The role and crew on `CurrentUser` belong to the active project, so a crew
 * comparison is only meaningful there. A delay from another assigned project
 * still lists, but it never counts as our crew's work.
 */
function isActiveProject(delay: DelaySummary, user: CurrentUser): boolean {
  return delay.projectId === user.activeProjectId;
}

function affectsOurCrew(delay: DelaySummary, user: CurrentUser): boolean {
  return (
    isActiveProject(delay, user) &&
    delay.blockedCrewNames.some((name) => coversCrew(user, name))
  );
}

function ownedByOurCrew(delay: DelaySummary, user: CurrentUser): boolean {
  return isActiveProject(delay, user) && coversCrew(user, delay.ownerCrewName);
}

const FILTER_RULES: Record<
  TrackingFilter,
  (delay: DelaySummary, user: CurrentUser) => boolean
> = {
  all: () => true,
  blocking: (delay, user) =>
    isDelayActive(delay) && affectsOurCrew(delay, user),
  waiting: (delay, user) => isDelayActive(delay) && ownedByOurCrew(delay, user),
  // Only the affected crew's Foreman reviews the proof of work for that crew.
  verify: (delay, user) =>
    delay.statusCode === "ReadyForHandover" &&
    isActiveProject(delay, user) &&
    canVerifyHandover(user, delay),
  closed: (delay) => !isDelayActive(delay),
};

/** Search matches the title, the FN-#### number, and the reason tag. */
function matchesSearch(delay: DelaySummary, search: string): boolean {
  if (search === "") {
    return true;
  }

  return [delay.title, delay.number, delay.tagName].some((value) =>
    value.toLowerCase().includes(search),
  );
}

/** A reopened or High priority delay is the most pressing row in the list. */
function isPressing(delay: DelaySummary): boolean {
  return delay.isHighPriority || delay.statusCode === "Reopened";
}

function byPressingThenNewest(left: DelaySummary, right: DelaySummary): number {
  const byPriority = Number(isPressing(right)) - Number(isPressing(left));

  return byPriority !== 0
    ? byPriority
    : Date.parse(right.createdAt) - Date.parse(left.createdAt);
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
  const term = search.trim().toLowerCase();

  return delays
    .filter(
      (delay) =>
        FILTER_RULES[filter](delay, user) && matchesSearch(delay, term),
    )
    .sort(byPressingThenNewest);
}

/** The delays of every project in scope, from one load. */
export interface TrackingData {
  delays: DelaySummary[];
  emptyMessage: string;
}

export type TrackingState = ApiQueryState<TrackingData>;

export type TrackingResultsView =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "ready"; delays: DelaySummary[]; emptyMessage?: string };

/** What the results card shows for the current request, tab, and search term. */
export function toResultsView(
  state: TrackingState,
  user: CurrentUser,
  filter: TrackingFilter,
  search: string,
): TrackingResultsView {
  if (state.status === "error") {
    return { kind: "error", message: state.message };
  }

  if (state.status !== "ready") {
    return { kind: "loading" };
  }

  const { delays, emptyMessage } = state.data;

  return {
    kind: "ready",
    delays: toVisibleDelays(delays, user, filter, search),
    emptyMessage: delays.length === 0 ? emptyMessage : undefined,
  };
}

/** The delays of the current request, whatever the tab and search term hide. */
export function toLoadedDelays(state: TrackingState): DelaySummary[] {
  return state.status === "ready" ? state.data.delays : [];
}

/** The workflow actions the detail view can offer, each with its own endpoint. */
export type DelayActionId =
  | "acknowledge"
  | "completionTime"
  | "requestCheck"
  | "verify"
  | "return"
  | "reopen"
  | "revoke";

/** One workflow action the screen offers the current user. */
export interface DelayActionOffer {
  id: DelayActionId;
  label: string;
  icon: LucideIcon;
}

interface ActionRule extends DelayActionOffer {
  isOffered: (delay: DelayDetail, user: CurrentUser) => boolean;
}

const ACTION_RULES: ActionRule[] = [
  {
    id: "acknowledge",
    label: "Acknowledge",
    icon: Wrench,
    isOffered: (delay, user) =>
      canAcknowledge(user, delay) &&
      (delay.statusCode === "Raised" || delay.statusCode === "Reopened"),
  },
  {
    id: "completionTime",
    label: "Revise completion time",
    icon: CalendarDays,
    isOffered: (delay, user) =>
      canSetCompletionTime(user, delay) && isDelayActive(delay),
  },
  {
    id: "requestCheck",
    label: "Submit for review",
    icon: Send,
    isOffered: (delay, user) =>
      canRequestHandoverCheck(user, delay) &&
      delay.statusCode === "Acknowledged",
  },
  {
    // Each affected crew closes the delay for itself; the last one closes it for everyone.
    id: "verify",
    label: "Accept for my crew",
    icon: CircleCheck,
    isOffered: (delay, user) =>
      canVerifyHandover(user, delay) &&
      delay.statusCode === "ReadyForHandover" &&
      verifiableImpacts(delay, user).length > 0,
  },
  {
    id: "return",
    label: "Send back",
    icon: Undo2,
    isOffered: (delay, user) =>
      canVerifyHandover(user, delay) &&
      delay.statusCode === "ReadyForHandover" &&
      verifiableImpacts(delay, user).length > 0,
  },
  {
    id: "reopen",
    label: "Reopen",
    icon: RotateCcw,
    isOffered: (delay, user) => canReopen(user, delay),
  },
  {
    id: "revoke",
    label: "Withdraw",
    icon: X,
    isOffered: (delay, user) =>
      canRevoke(user, delay.reportedBy.crewName) && isDelayActive(delay),
  },
];

/**
 * The actions this user is entitled to on this delay. The API is the authority
 * on every one of them and rejects anything these rules let through.
 */
export function toOfferedActions(
  delay: DelayDetail,
  user: CurrentUser,
): DelayActionOffer[] {
  if (delay.projectId !== user.activeProjectId) {
    return [];
  }

  return ACTION_RULES.filter((rule) => rule.isOffered(delay, user)).map(
    ({ id, label, icon }) => ({ id, label, icon }),
  );
}

/** One stage of the handover, with the crew and role it waits on. */
export interface HandoverStep {
  id: string;
  label: string;
  actor: string;
  state: "done" | "current" | "pending";
}

function affectedCrewLabel(delay: DelayDetail): string {
  return delay.blockedCrewNames.length === 0
    ? "the affected crew"
    : delay.blockedCrewNames.join(", ");
}

/**
 * The stage the current cycle sits in. A send-back returns the delay to
 * Acknowledged, so it reads as waiting for a new submission. A reopened cycle
 * starts the stages again, which is why it reads as the first.
 */
function currentStageIndex(delay: DelayDetail): number {
  switch (delay.statusCode) {
    case "Acknowledged":
      return 2;
    case "ReadyForHandover":
      return 3;
    case "Resolved":
      return 4;
    case "Raised":
    case "Reopened":
    case "Revoked":
      return 1;
  }
}

/** The four review stages of the current cycle, earliest first. */
export function toHandoverSteps(delay: DelayDetail): HandoverStep[] {
  const stages = [
    {
      id: "raised",
      label: "Delay raised",
      actor: `${delay.reportedBy.crewName} · ${delay.reportedBy.displayName}`,
    },
    {
      id: "acknowledge",
      label: "Acknowledge",
      actor: `${delay.ownerCrewName} · Superintendent`,
    },
    {
      id: "review",
      label: "Submit proof of work",
      actor: `${delay.ownerCrewName} · Superintendent or Foreman`,
    },
    {
      id: "accept",
      label: "Review and accept",
      actor: `${affectedCrewLabel(delay)} · Foreman`,
    },
  ];
  const current = currentStageIndex(delay);
  const isClosed = delay.statusCode === "Resolved";

  return stages.map((stage, index) => ({
    ...stage,
    state:
      isClosed || index < current
        ? "done"
        : index === current
          ? "current"
          : "pending",
  }));
}

/**
 * What this user does next on this delay. The sentence follows the same
 * permission helpers the offered actions use, so the two never disagree.
 */
export function toNextStepNote(delay: DelayDetail, user: CurrentUser): string {
  if (delay.projectId !== user.activeProjectId) {
    return `This delay belongs to ${delay.projectName}. Switch to that project to act on it.`;
  }

  if (delay.statusCode === "Revoked") {
    return "The reporting Foreman’s Superintendent withdrew this delay. Nothing is outstanding.";
  }

  if (delay.statusCode === "Resolved") {
    return canReopen(user, delay)
      ? "Every affected crew accepted the proof of work, so this delay is closed. Reopen it if the blocker is still on site."
      : "Every affected crew accepted the proof of work, so this delay is closed. Any Superintendent can reopen it if the blocker remains.";
  }

  if (
    (delay.statusCode === "Raised" || delay.statusCode === "Reopened") &&
    canAcknowledge(user, delay)
  ) {
    return "Speak to your Foreman, then acknowledge this delay and give a tentative completion time if work remains.";
  }

  if (
    delay.statusCode === "Acknowledged" &&
    canRequestHandoverCheck(user, delay)
  ) {
    return `Once the work is done, attach proof-of-work photos and submit this delay for ${affectedCrewLabel(delay)} to review.`;
  }

  if (
    delay.statusCode === "ReadyForHandover" &&
    canVerifyHandover(user, delay)
  ) {
    return verifiableImpacts(delay, user).length > 0
      ? "Look at the proof of work and check the site. Accept it to close this delay for your crew, or send it back with what to fix."
      : "You accepted the proof of work for your crew. The delay closes when every affected crew has accepted.";
  }

  if (
    delay.statusCode === "ReadyForHandover" &&
    canRequestHandoverCheck(user, delay)
  ) {
    return `Submitted for review. ${affectedCrewLabel(delay)} must accept the proof of work; you will be notified when the delay closes.`;
  }

  return `Nothing waits on you. ${delay.ownerCrewName} must remove the blocker.`;
}
