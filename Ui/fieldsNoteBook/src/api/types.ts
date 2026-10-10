import type { DelayStatusCode, DelayTagCode, DelayReportType } from '@/domain/delays'
import type { RoleName, ThemePreference } from '@/domain/users'

/** Every client function accepts this, so a page can cancel an obsolete call. */
export interface RequestOptions {
  signal?: AbortSignal
}

/**
 * Table-shaped rows, named exactly as the database columns are. The fixtures
 * hold these, the client modules map them to the DTOs in `@/domain`, and the
 * real API will replace only the first half of that trip.
 */

export interface DelayRow {
  Id: number
  Number: string
  ProjectId: number
  Title: string
  Description: string | null
  ReportedByUserId: number
  ReportedByCrewId: number
  OwnerCrewId: number
  DelayTagId: number
  ReportType: DelayReportType
  BlockingWorkItemId: number | null
  DelayStatusId: number
  SegmentId: number | null
  SiteMapId: number | null
  PinX: number | null
  PinY: number | null
  LocationSource: string | null
  LocationName: string | null
  CreatedAt: string
}

export interface DelayCycleRow {
  DelayCycleId: number
  DelayId: number
  CreatedAt: string
  CreatedBy: number
  ModifiedAt: string | null
  ModifiedBy: number | null
  OpenReason: string | null
  AcknowledgmentChoice: string | null
  AcknowledgedByUserId: number | null
  AcknowledgedAt: string | null
  AcknowledgmentNote: string | null
  ExpectedReadyAt: string | null
  ClosedByUserId: number | null
  ClosedAt: string | null
}

export interface DelayImpactRow {
  Id: number
  DelayId: number
  ProjectId: number
  BlockedCrewId: number
  BlockedWorkItemId: number | null
}

export type DelayPhotoPurpose = 'InitialReport' | 'ReopenEvidence' | 'ProofOfWork'

export interface DelayPhotoRow {
  Id: number
  DelayId: number
  DelayCycleId: number
  Purpose: DelayPhotoPurpose
  CreatedByUserId: number
  UpdatedByUserId: number | null
  OriginalUrl: string
  ThumbnailUrl: string
  FileName: string
  CreatedAt: string
  UpdatedAt: string | null
  Latitude: number | null
  Longitude: number | null
}

export interface DelayStatusRow {
  Id: number
  Code: DelayStatusCode
  Name: string
  IsActive: boolean
}

export interface DelayTagRow {
  Id: number
  Code: DelayTagCode
  Name: string
  IsActive: boolean
}

export interface UserRow {
  Id: number
  DisplayName: string
  Email: string
  Phone: string | null
  IsActive: boolean
}

export interface UserSettingsRow {
  UserId: number
  ThemePreference: ThemePreference
  EmailEnabled: boolean
  SmsEnabled: boolean
}

export interface UserRoleRow {
  UserRoleId: number
  UserId: number
  RoleId: number
  ProjectId: number
  IsActive: boolean
}

export interface UserProjectRow {
  Id: number
  UserId: number
  ProjectId: number
  IsActive: boolean
}

export interface RoleRow {
  Id: number
  Name: RoleName
  IsActive: boolean
}

export interface CrewRow {
  Id: number
  ProjectId: number
  Name: string
  ForemanLeadId: number | null
  SuperintendentUserId: number | null
  IsActive: boolean
}

export interface ProjectRow {
  Id: number
  DistrictId: number
  Name: string
  Code: string
  IsActive: boolean
}

export interface DistrictRow {
  Id: number
  Name: string
  Code: string
  IsActive: boolean
}

export interface SegmentRow {
  Id: number
  ProjectId: number
  Name: string
  IsActive: boolean
}

export interface SiteMapRow {
  Id: number
  ProjectId: number
  Name: string
  Uri: string
  IsActive: boolean
}

export interface ActivityEventRow {
  Id: number
  ProjectId: number
  DelayId: number
  DelayCycleId: number | null
  DelayImpactId: number | null
  ActorUserId: number
  /** Dotted string such as `delay.raised`. Treat it as open. */
  EventType: string
  FromDelayStatusId: number | null
  ToDelayStatusId: number | null
  OldExpectedReadyAt: string | null
  NewExpectedReadyAt: string | null
  CreatedAt: string
}

export interface WorkItemRow {
  Id: number
  ProjectId: number
  Title: string
  SegmentId: number | null
  PlannedStartAt: string | null
  PlannedFinishAt: string | null
  ExpectedFinishAt: string | null
  IsCompleted: boolean
}

export interface WorkItemTeamAssignmentRow {
  Id: number
  ProjectId: number
  WorkItemId: number
  CrewId: number
}
