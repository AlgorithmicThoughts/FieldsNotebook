import {
  Bell,
  CalendarDays,
  ChartNoAxesCombined,
  FileText,
  House,
  ListTodo,
  MapPin,
  Settings,
} from 'lucide-react'
import type { NavItem } from '../../components/layout/Sidebar'
import type { ActivityEntry } from './components/ActivityTimeline'

// Sample values until the API supplies them.

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
