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

/** One delay as a list row shows it. A delay can affect several crews. */
export interface DelaySummary {
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
  projectName: string
  /** The delay is in a reopened cycle, so it is pinned and High priority. */
  isHighPriority: boolean
}
