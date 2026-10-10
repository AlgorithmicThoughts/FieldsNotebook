import {
  Bell,
  CalendarDays,
  ChartNoAxesCombined,
  CirclePause,
  ClipboardCheck,
  FileText,
  House,
  ListTodo,
  MapPin,
  Settings,
  Users,
} from 'lucide-react'
import type { DelaySummary } from '../../domain/delays'
import type { NavItem } from '../../components/layout/Sidebar'
import type { ActivityEntry } from './components/ActivityTimeline'
import type { AdvanceNotice } from './components/AdvanceNoticeHero'
import type { HomeMetric } from './components/MetricCard'

// Sample values until the API supplies them.

export const LAST_CHECKED_LABEL = '08:42 AM'
export const HAS_UNREAD_NOTIFICATIONS = true

export const NAV_ITEMS: NavItem[] = [
  { id: 'home', label: 'Home', icon: House },
  { id: 'tracking', label: 'Tracking', icon: ListTodo },
  { id: 'site-map', label: 'Site map', icon: MapPin },
  { id: 'documents', label: 'Documents', icon: FileText },
  { id: 'upcoming', label: 'Upcoming', icon: CalendarDays },
  { id: 'notifications', label: 'Notifications', icon: Bell, count: 2 },
  { id: 'reports', label: 'Reports', icon: ChartNoAxesCombined },
  { id: 'settings', label: 'Settings', icon: Settings },
]

export const ADVANCE_NOTICE: AdvanceNotice = {
  delayNumber: 'FN-1042',
  headline: 'Crew B cannot start the weld as planned.',
  detail:
    'Crew A reports a delay to the Frame 12 fit-up. Your Superintendent coordinates the arrival plan.',
  ownerCrewName: 'Crew A',
  blockedCrewNames: ['Crew B'],
  expectedReadyTimeLabel: '2:00 PM',
  expectedReadyDayLabel: 'Tomorrow · 8 October',
}

// The API supplies these counts for every active delay, not only the rows below.
export const HOME_METRICS: HomeMetric[] = [
  { id: 'blocking', icon: CirclePause, value: 3, label: 'Blocking our crew' },
  { id: 'dependent', icon: Users, value: 1, label: 'Crews waiting on us' },
  { id: 'verify', icon: ClipboardCheck, value: 1, label: 'Ready to verify' },
]

export const ATTENTION_DELAYS: DelaySummary[] = [
  {
    number: 'FN-1034',
    title: 'Access to the fit-up area is blocked again',
    statusCode: 'Reopened',
    tagCode: 'SafetyIssue',
    tagName: 'Safety issue',
    ownerCrewName: 'Crew B',
    blockedCrewNames: ['Crew A'],
    locationName: 'East access lane',
    projectName: 'Omaha Fabrication',
    isHighPriority: true,
  },
  {
    number: 'FN-1042',
    title: 'Frame fit-up will delay tomorrow’s weld',
    statusCode: 'Acknowledged',
    tagCode: 'PlanConflict',
    tagName: 'Plan conflict',
    ownerCrewName: 'Crew A',
    blockedCrewNames: ['Crew B'],
    locationName: 'Bay 4 · Frame 12',
    projectName: 'Omaha Fabrication',
    isHighPriority: false,
  },
  {
    number: 'FN-1040',
    title: 'Overhead crane is out of service',
    statusCode: 'Raised',
    tagCode: 'EquipmentBreakdown',
    tagName: 'Equipment breakdown',
    ownerCrewName: 'Crew A',
    blockedCrewNames: ['Crew B', 'Crew C'],
    locationName: 'Bay 2 · Overhead crane',
    projectName: 'Omaha Fabrication',
    isHighPriority: false,
  },
  {
    number: 'FN-1038',
    title: 'Frame 08 awaits handover verification',
    statusCode: 'ReadyForHandover',
    tagCode: 'PendingInspection',
    tagName: 'Pending inspection',
    ownerCrewName: 'Crew A',
    blockedCrewNames: ['Crew B'],
    locationName: 'Bay 3 · Frame 08',
    projectName: 'Omaha Fabrication',
    isHighPriority: false,
  },
]

export const ACTIVITY_ENTRIES: ActivityEntry[] = [
  {
    id: '4821',
    title: 'Access delay reopened',
    detail: 'Crew B must acknowledge again.',
    timeLabel: '08:35 AM',
    delayNumber: 'FN-1034',
  },
  {
    id: '4818',
    title: 'Frame 08 ready for verification',
    detail: 'Crew B’s Foreman takes the next step.',
    timeLabel: '08:30 AM',
    delayNumber: 'FN-1038',
  },
  {
    id: '4815',
    title: 'Tomorrow’s delay confirmed',
    detail: 'Crew A estimates 2:00 PM.',
    timeLabel: '08:22 AM',
    delayNumber: 'FN-1042',
  },
]
