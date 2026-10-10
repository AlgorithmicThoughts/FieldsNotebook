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
} from 'lucide-react'
import type { RoleName } from './users'

/** Status codes from the approved DelayStatus contract. */
export type DelayStatusCode =
  | 'Raised'
  | 'Acknowledged'
  | 'ReadyForHandover'
  | 'Resolved'
  | 'Reopened'
  | 'Revoked'

/** Reason codes from the approved DelayTags contract. */
export type DelayTagCode =
  | 'MissingMaterial'
  | 'SafetyIssue'
  | 'PersonnelShortage'
  | 'QualityConcern'
  | 'PlanConflict'
  | 'PendingInspection'
  | 'EquipmentBreakdown'
  | 'AdverseWeather'
  | 'Other'

export type StatusTone =
  | 'neutral'
  | 'blocked'
  | 'progress'
  | 'verify'
  | 'resolved'
  | 'urgent'

export interface DelayStatusDisplay {
  label: string
  tone: StatusTone
  icon: LucideIcon
}

/** Display state for each status code. These are not database values. */
export const DELAY_STATUS_DISPLAY: Record<DelayStatusCode, DelayStatusDisplay> = {
  Raised: { label: 'Blocked', tone: 'blocked', icon: CirclePause },
  Acknowledged: { label: 'In progress', tone: 'progress', icon: Wrench },
  ReadyForHandover: { label: 'Ready to verify', tone: 'verify', icon: ClipboardCheck },
  Resolved: { label: 'Resolved', tone: 'resolved', icon: CircleCheck },
  Reopened: { label: 'Reopened', tone: 'urgent', icon: RotateCcw },
  Revoked: { label: 'Revoked', tone: 'neutral', icon: X },
}

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
}

/** Delays.ReportType. An advance notice is raised before the crew arrives. */
export type DelayReportType = 'AfterArrival' | 'AdvanceNotice'

/** Normalised 0-1 position on the project site map. */
export interface DelayPin {
  x: number
  y: number
}

/** One delay as a list row shows it. A delay can affect several crews. */
export interface DelaySummary {
  /** Delays.Id. Needed for detail lookups and stable keys. */
  id: number
  /** Delays.Number, such as FN-1042. */
  number: string
  title: string
  statusCode: DelayStatusCode
  tagCode: DelayTagCode
  /** DelayTags.Name. */
  tagName: string
  /** Crew that must remove the blocker. */
  ownerCrewName: string
  /** Every crew that DelayImpacts records for this delay. */
  blockedCrewNames: string[]
  /** Segments.Name, or Delays.LocationName when the user typed one. */
  locationName: string
  projectId: number
  projectName: string
  /** The delay is in a reopened cycle, so it is pinned and High priority. */
  isHighPriority: boolean
  reportType: DelayReportType
  /** Delays.CreatedAt, ISO 8601 UTC. Format at the edge, never in the DTO. */
  createdAt: string
  /** DelayCycles.ExpectedReadyAt for the active cycle, or null. Tentative. */
  expectedReadyAt: string | null
  /** Position on the project site map, or null when the delay has no pin. */
  pin: DelayPin | null
  /** Delays.SiteMapId. */
  siteMapId: number | null
  /** Segments.Name, or null when the delay sits outside a segment. */
  segmentName: string | null
}

/**
 * Delays.LocationSource. A client only ever writes a position the user chose.
 * Photo GPS never becomes a pin, because the site image has no calibration.
 */
export type DelayLocationSource = 'ManualPin' | 'ManualEntry'

/** One affected crew and the work item it cannot start. */
export interface DelayImpactRequest {
  /** DelayImpacts.BlockedCrewId. */
  blockedCrewId: number
  /** DelayImpacts.BlockedWorkItemId. */
  blockedWorkItemId: number | null
}

/** One attached photo, with EXIF GPS only when the file carried it. */
export interface DelayPhotoUpload {
  file: File
  /** DelayPhotos.Latitude. */
  latitude: number | null
  /** DelayPhotos.Longitude. */
  longitude: number | null
}

/** Everything the API needs to raise a delay in one transaction. */
export interface CreateDelayRequest {
  projectId: number
  /** Delays.Description. Delays.Title follows from it. */
  description: string
  /** Delays.OwnerCrewId, the crew that must remove the blocker. */
  ownerCrewId: number
  /** One DelayImpacts row per affected crew. The approved model permits many. */
  impacts: DelayImpactRequest[]
  /** Delays.BlockingWorkItemId. */
  blockingWorkItemId: number | null
  /** Delays.DelayTagId. Exactly one cause, never a status or a priority. */
  delayTagId: number
  reportType: DelayReportType
  /** DelayCycles.ExpectedReadyAt for the first cycle. Tentative, never binding. */
  expectedReadyAt: string | null
  /** DelayPhotos rows with Purpose InitialReport. One to five. */
  photos: DelayPhotoUpload[]
  segmentId: number | null
  siteMapId: number | null
  pin: DelayPin | null
  locationSource: DelayLocationSource | null
  /** Delays.LocationName, up to 160 characters. */
  locationName: string | null
  /** Extra Notifications recipients beyond the responsible Superintendent. */
  notifyUserIds: number[]
}

/** A crew in the active project. */
export interface CrewOption {
  id: number
  name: string
}

/** A planned task, with the crews it is assigned to. */
export interface WorkItemOption {
  id: number
  title: string
  /** WorkItemTeamAssignment.CrewId values. */
  crewIds: number[]
}

/** A named work area in the active project. */
export interface SegmentOption {
  id: number
  name: string
}

/** One approved reason tag, as DelayTags stores it. */
export interface DelayTagOption {
  id: number
  code: DelayTagCode
  name: string
}

/** The project site image a pin is normalised against. */
export interface SiteMapOption {
  id: number
  name: string
  uri: string
}

/** Someone the reporter can add to the delay's notification recipients. */
export interface NotifyPersonOption {
  userId: number
  displayName: string
  crewId: number
  crewName: string
  roleName: RoleName
}

/** Reference data the report form offers, already scoped to one project. */
export interface DelayFormOptions {
  projectId: number
  projectName: string
  crews: CrewOption[]
  workItems: WorkItemOption[]
  segments: SegmentOption[]
  tags: DelayTagOption[]
  siteMap: SiteMapOption | null
  people: NotifyPersonOption[]
}

const TITLE_MAX_LENGTH = 90

/**
 * Delays.Title is not a form field. It follows the first sentence of the
 * description so a list row still reads as a sentence.
 */
export function deriveDelayTitle(description: string): string {
  const text = description.trim().replace(/\s+/g, ' ')
  const sentenceEnd = text.search(/[.!?](\s|$)/)
  const sentence = sentenceEnd === -1 ? text : text.slice(0, sentenceEnd)

  if (sentence.length <= TITLE_MAX_LENGTH) {
    return sentence
  }

  return `${sentence.slice(0, TITLE_MAX_LENGTH - 1).trimEnd()}…`
}
