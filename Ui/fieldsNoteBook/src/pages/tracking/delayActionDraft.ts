import {
  acknowledgeDelay,
  reopenDelay,
  requestHandoverCheck,
  returnHandover,
  reviseCompletionTime,
  revokeDelay,
  verifyHandover,
  type DelayActionResult,
} from "@/api/delayActions";
import type { RequestOptions } from "@/api/types";
import type {
  DelayAcknowledgmentChoice,
  DelayDetail,
  DelayImpactDetail,
} from "@/domain/delays";
import { coversCrew, type CurrentUser } from "@/domain/users";
import {
  MAX_PHOTOS,
  MIN_PHOTOS,
  type PhotoDraft,
} from "@/components/delays/delayDraft";
import type { DelayActionId } from "./trackingView";

/** Everything the action forms collect. One action reads only the fields it needs. */
export interface DelayActionDraft {
  choice: DelayAcknowledgmentChoice;
  note: string;
  /** A `datetime-local` value in project time, converted to UTC on submit. */
  expectedReadyAt: string;
  /** DelayImpacts.Id the Foreman accepts or sends back, chosen from their own crew's impacts. */
  impactId: number | null;
  /** Why the delay reopens, or what the responsible crew must fix after a send-back. */
  reason: string;
  /** Proof-of-work photos the affected crews review. Only a submission for review sends them. */
  photos: PhotoDraft[];
}

export const EMPTY_ACTION_DRAFT: DelayActionDraft = {
  choice: "WorkRemains",
  note: "",
  expectedReadyAt: "",
  impactId: null,
  reason: "",
  photos: [],
};

/**
 * The impacts this Foreman can still review: their own crew's, not yet accepted
 * in the current cycle. A reopened cycle returns every one of them again.
 */
export function verifiableImpacts(
  delay: DelayDetail,
  user: CurrentUser,
): DelayImpactDetail[] {
  return delay.impacts.filter(
    (impact) =>
      coversCrew(user, impact.crewName) && impact.verifications.length === 0,
  );
}

/** The tentative completion time only applies while work remains. */
export function needsCompletionTime(draft: DelayActionDraft): boolean {
  return draft.choice === "WorkRemains";
}

/** A `datetime-local` value is project-local wall time. The contract is UTC. */
function toUtcIso(value: string): string | null {
  const parsed = new Date(value);

  return value === "" || Number.isNaN(parsed.getTime())
    ? null
    : parsed.toISOString();
}

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();

  return trimmed === "" ? null : trimmed;
}

/**
 * What the form still needs before it can submit, or null when it is complete.
 * The API repeats every one of these checks and remains the authority.
 */
export function validateActionDraft(
  action: DelayActionId,
  draft: DelayActionDraft,
): string | null {
  if (
    action === "acknowledge" &&
    needsCompletionTime(draft) &&
    toUtcIso(draft.expectedReadyAt) === null
  ) {
    return "Enter the tentative completion date and time.";
  }

  if (action === "completionTime" && toUtcIso(draft.expectedReadyAt) === null) {
    return "Enter the revised completion date and time.";
  }

  if ((action === "verify" || action === "return") && draft.impactId === null) {
    return "Choose the affected crew you are reviewing for.";
  }

  if (action === "return" && trimmedOrNull(draft.reason) === null) {
    return "Say what the responsible crew must fix.";
  }

  if (action === "reopen" && trimmedOrNull(draft.reason) === null) {
    return "Say why this delay is reopening.";
  }

  if (
    action === "requestCheck" &&
    (draft.photos.length < MIN_PHOTOS || draft.photos.length > MAX_PHOTOS)
  ) {
    return `Add ${MIN_PHOTOS} to ${MAX_PHOTOS} proof-of-work photos.`;
  }

  return null;
}

/** Sends one validated action to its endpoint. */
export function submitDelayAction(
  action: DelayActionId,
  delay: DelayDetail,
  draft: DelayActionDraft,
  options?: RequestOptions,
): Promise<DelayActionResult> {
  const { projectId, id } = delay;

  switch (action) {
    case "acknowledge":
      return acknowledgeDelay(
        projectId,
        id,
        {
          choice: draft.choice,
          note: trimmedOrNull(draft.note),
          expectedReadyAt: needsCompletionTime(draft)
            ? toUtcIso(draft.expectedReadyAt)
            : null,
        },
        options,
      );
    case "completionTime":
      return reviseCompletionTime(
        projectId,
        id,
        toUtcIso(draft.expectedReadyAt)!,
        options,
      );
    case "requestCheck":
      return requestHandoverCheck(
        projectId,
        id,
        draft.photos.map(({ file, latitude, longitude }) => ({
          file,
          latitude,
          longitude,
        })),
        options,
      );
    case "verify":
      return verifyHandover(
        projectId,
        id,
        { impactId: draft.impactId!, note: trimmedOrNull(draft.note) },
        options,
      );
    case "return":
      return returnHandover(
        projectId,
        id,
        { impactId: draft.impactId!, reason: draft.reason.trim() },
        options,
      );
    case "reopen":
      return reopenDelay(projectId, id, draft.reason.trim(), options);
    case "revoke":
      return revokeDelay(projectId, id, options);
  }
}
