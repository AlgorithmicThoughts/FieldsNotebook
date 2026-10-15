import {
  CirclePause,
  CircleCheck,
  ClipboardCheck,
  CloudRain,
  FileText,
  Images,
  Info,
  RotateCcw,
  ShieldCheck,
  Users,
  Wrench,
  X,
  type LucideIcon,
} from "lucide-react";
import type { RoleName } from "./users";

/** Status codes from the approved DelayStatus contract. */
export type DelayStatusCode =
  | "Raised"
  | "Acknowledged"
  | "ReadyForHandover"
  | "Resolved"
  | "Reopened"
  | "Revoked";

/** Reason codes from the approved DelayTags contract. */
export type DelayTagCode =
  | "MissingMaterial"
  | "SafetyIssue"
  | "PersonnelShortage"
  | "QualityConcern"
  | "PlanConflict"
  | "PendingInspection"
  | "EquipmentBreakdown"
  | "AdverseWeather"
  | "Other";

export type StatusTone =
  | "neutral"
  | "blocked"
  | "progress"
  | "verify"
  | "resolved"
  | "urgent";

export interface DelayStatusDisplay {
  label: string;
  tone: StatusTone;
  icon: LucideIcon;
}

/** Display state for each status code. These are not database values. */
export const DELAY_STATUS_DISPLAY: Record<DelayStatusCode, DelayStatusDisplay> =
  {
    Raised: { label: "Blocked", tone: "blocked", icon: CirclePause },
    Acknowledged: { label: "In progress", tone: "progress", icon: Wrench },
    ReadyForHandover: {
      label: "Ready to verify",
      tone: "verify",
      icon: ClipboardCheck,
    },
    Resolved: { label: "Resolved", tone: "resolved", icon: CircleCheck },
    Reopened: { label: "Reopened", tone: "urgent", icon: RotateCcw },
    Revoked: { label: "Revoked", tone: "neutral", icon: X },
  };

export const DELAY_TAG_ICON: Record<DelayTagCode, LucideIcon> = {
  MissingMaterial: Images,
  SafetyIssue: ShieldCheck,
  PersonnelShortage: Users,
  QualityConcern: ClipboardCheck,
  PlanConflict: FileText,
  PendingInspection: ClipboardCheck,
  EquipmentBreakdown: Wrench,
  AdverseWeather: CloudRain,
  Other: Info,
};

/** Delays.ReportType. An advance notice is raised before the crew arrives. */
export type DelayReportType = "AfterArrival" | "AdvanceNotice";

/**
 * DelayCycles.AcknowledgmentChoice. The column is an open 30-character string;
 * these are the prepared answers the acknowledgment form offers.
 */
export type DelayAcknowledgmentChoice = "WorkRemains" | "ReadyForHandover";

export const DELAY_ACKNOWLEDGMENT_CHOICES: {
  value: DelayAcknowledgmentChoice;
  label: string;
}[] = [
  { value: "WorkRemains", label: "My crew will clear it" },
  { value: "ReadyForHandover", label: "The work is already done" },
];

/** DelayPhotos.Purpose. */
export type DelayPhotoPurpose =
  | "InitialReport"
  | "ReopenEvidence"
  | "ProofOfWork";

/** The order a detail view shows the photo sets in, earliest purpose first. */
export const DELAY_PHOTO_PURPOSE_ORDER: DelayPhotoPurpose[] = [
  "InitialReport",
  "ReopenEvidence",
  "ProofOfWork",
];

export const DELAY_PHOTO_PURPOSE_LABEL: Record<DelayPhotoPurpose, string> = {
  InitialReport: "Initial report",
  ReopenEvidence: "Reopen evidence",
  ProofOfWork: "Proof of work",
};

/** Normalised 0-1 position on the project site map. */
export interface DelayPin {
  x: number;
  y: number;
}

/**
 * One DelayImpacts row as a list needs it: the crew that cannot work and the
 * work item it cannot start. Every affected crew keeps its own row.
 */
export interface DelayImpactSummary {
  /** DelayImpacts.Id. */
  id: number;
  crewName: string;
  /** DelayImpacts.BlockedWorkItemId, or null when no work item was named. */
  workItemId: number | null;
}

/** One delay as a list row shows it. A delay can affect several crews. */
export interface DelaySummary {
  /** Delays.Id. Needed for detail lookups and stable keys. */
  id: number;
  /** Delays.Number, such as FN-1042. */
  number: string;
  title: string;
  statusCode: DelayStatusCode;
  tagCode: DelayTagCode;
  /** DelayTags.Name. */
  tagName: string;
  /** Crew that must remove the blocker. */
  ownerCrewName: string;
  /** Every crew that DelayImpacts records for this delay. */
  blockedCrewNames: string[];
  /** Every DelayImpacts row, so a caller can pair a crew with its work item. */
  impacts: DelayImpactSummary[];
  /** Segments.Name, or Delays.LocationName when the user typed one. */
  locationName: string;
  projectId: number;
  projectName: string;
  /** The delay is in a reopened cycle, so it is pinned and High priority. */
  isHighPriority: boolean;
  reportType: DelayReportType;
  /** Delays.CreatedAt, ISO 8601 UTC. Format at the edge, never in the DTO. */
  createdAt: string;
  /** DelayCycles.ExpectedReadyAt for the active cycle, or null. Tentative. */
  expectedReadyAt: string | null;
  /** Position on the project site map, or null when the delay has no pin. */
  pin: DelayPin | null;
  /** Delays.SiteMapId. */
  siteMapId: number | null;
  /** Segments.Name, or null when the delay sits outside a segment. */
  segmentName: string | null;
}

/** A delay stays active until it is resolved or withdrawn. Time alone never clears it. */
export function isDelayActive(delay: DelaySummary): boolean {
  return delay.statusCode !== "Resolved" && delay.statusCode !== "Revoked";
}

/** The author of the delay, from Delays.ReportedBy* and Delays.CreatedAt. */
export interface DelayReporter {
  userId: number;
  displayName: string;
  crewName: string;
  /** ISO 8601 UTC. Format at the edge, never in the DTO. */
  reportedAt: string;
}

/** One DelayCycles row. The report opens cycle one; each reopen adds another. */
export interface DelayCycleDetail {
  /** DelayCycles.DelayCycleId. */
  id: number;
  /** 1 for the initial report, then 2, 3 … per reopen. Derived, not stored. */
  sequence: number;
  createdAt: string;
  /** DelayCycles.OpenReason. Only a reopened cycle carries one. */
  openReason: string | null;
  acknowledgmentChoice: string | null;
  acknowledgmentNote: string | null;
  acknowledgedAt: string | null;
  /** Users.DisplayName for DelayCycles.AcknowledgedByUserId. */
  acknowledgedByName: string | null;
  /** Tentative completion time for this cycle, or null. */
  expectedReadyAt: string | null;
  closedAt: string | null;
}

/** One DelayPhotos row. A photo without GPS simply has no coordinates. */
export interface DelayPhotoDetail {
  id: number;
  originalUrl: string;
  /** Fits within 400 by 400 pixels and preserves proportions. */
  thumbnailUrl: string;
  fileName: string;
  createdAt: string;
  latitude: number | null;
  longitude: number | null;
}

/** The photos of one purpose, so each set carries its own heading. */
export interface DelayPhotoGroup {
  purpose: DelayPhotoPurpose;
  photos: DelayPhotoDetail[];
}

/** One affected crew accepting the proof of work, from DelayImpactVerifications. */
export interface DelayVerification {
  verifiedByName: string;
  verifiedAt: string;
  note: string | null;
}

/** One affected crew, the work item it cannot start, and its checks. */
export interface DelayImpactDetail extends DelayImpactSummary {
  /** WorkItems.Title for DelayImpacts.BlockedWorkItemId, or null. */
  workItemTitle: string | null;
  /** Checks for the current cycle only. A reopened cycle needs new ones. */
  verifications: DelayVerification[];
}

/** One ActivityEvents row as the delay history shows it. */
export interface DelayEventDetail {
  id: number;
  /** Open dotted string such as `delay.raised`. */
  eventType: string;
  actorName: string;
  createdAt: string;
  /** Cycle the event belongs to, or null when it has none. */
  cycleSequence: number | null;
  oldExpectedReadyAt: string | null;
  newExpectedReadyAt: string | null;
  /** Why the proof of work was sent back; only on `delay.handoverReturned`. */
  returnReason: string | null;
}

/** One delay with the history, impacts, and photos the detail view needs. */
export interface DelayDetail extends DelaySummary {
  /** Delays.Description. */
  description: string | null;
  reportedBy: DelayReporter;
  /** Oldest first. Cycle history is preserved, never overwritten. */
  cycles: DelayCycleDetail[];
  photos: DelayPhotoGroup[];
  /** Every affected crew DelayImpacts records. Never reduced to one. */
  impacts: DelayImpactDetail[];
  /** Oldest first. */
  events: DelayEventDetail[];
}

/**
 * Delays.LocationSource. A client only ever writes a position the user chose.
 * Photo GPS never becomes a pin, because the site image has no calibration.
 */
export type DelayLocationSource = "ManualPin" | "ManualEntry";

/** One affected crew and the work item it cannot start. */
export interface DelayImpactRequest {
  /** DelayImpacts.BlockedCrewId. */
  blockedCrewId: number;
  /** DelayImpacts.BlockedWorkItemId. */
  blockedWorkItemId: number | null;
}

/** One attached photo, with EXIF GPS only when the file carried it. */
export interface DelayPhotoUpload {
  file: File;
  /** DelayPhotos.Latitude. */
  latitude: number | null;
  /** DelayPhotos.Longitude. */
  longitude: number | null;
}

/** Everything the API needs to raise a delay in one transaction. */
export interface CreateDelayRequest {
  projectId: number;
  title: string;
  /** Delays.Description. */
  description: string;
  /** Delays.OwnerCrewId, the crew that must remove the blocker. */
  ownerCrewId: number;
  /** One DelayImpacts row per affected crew. The approved model permits many. */
  impacts: DelayImpactRequest[];
  /** Delays.BlockingWorkItemId. */
  blockingWorkItemId: number | null;
  /** Delays.DelayTagId. Exactly one cause, never a status or a priority. */
  delayTagId: number;
  reportType: DelayReportType;
  /** DelayPhotos rows with Purpose InitialReport. One to five. */
  photos: DelayPhotoUpload[];
  segmentId: number | null;
  siteMapId: number | null;
  pin: DelayPin | null;
  locationSource: DelayLocationSource | null;
  /** Delays.LocationName, up to 160 characters. */
  locationName: string | null;
  /** Extra Notifications recipients beyond the responsible Superintendent. */
  notifyUserIds: number[];
}

/** A crew in the active project. */
export interface CrewOption {
  id: number;
  name: string;
}

/** A planned task, with the crews it is assigned to. */
export interface WorkItemOption {
  id: number;
  title: string;
  /** WorkItemTeamAssignment.CrewId values. */
  crewIds: number[];
}

/** A named work area in the active project. */
export interface SegmentOption {
  id: number;
  name: string;
}

/** One approved reason tag, as DelayTags stores it. */
export interface DelayTagOption {
  id: number;
  code: DelayTagCode;
  name: string;
}

/** The project site image a pin is normalised against. */
export interface SiteMapOption {
  id: number;
  name: string;
  uri: string;
}

/** Someone the reporter can add to the delay's notification recipients. */
export interface NotifyPersonOption {
  userId: number;
  displayName: string;
  crewId: number;
  crewName: string;
  roleName: RoleName;
}

/** Reference data the report form offers, already scoped to one project. */
export interface DelayFormOptions {
  projectId: number;
  projectName: string;
  crews: CrewOption[];
  workItems: WorkItemOption[];
  segments: SegmentOption[];
  tags: DelayTagOption[];
  siteMap: SiteMapOption | null;
  people: NotifyPersonOption[];
}

/** Editable report fields suggested from a recording. */
export interface DelaySpeechResponse {
  title: string
  description: string
  ownerCrewId: number | null
  delayTagId: number | null
}
