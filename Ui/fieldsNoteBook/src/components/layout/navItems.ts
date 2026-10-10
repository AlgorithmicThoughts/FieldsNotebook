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
import type { NavItem } from './Sidebar'

export const NOTIFICATIONS_NAV_ID = 'notifications'

/** Primary navigation; the Notifications count comes from the unread-count request. */
export function buildNavItems(unreadNotificationCount: number): NavItem[] {
  return [
    { id: 'home', label: 'Home', icon: House },
    { id: 'tracking', label: 'Tracking', icon: ListTodo },
    { id: 'site-map', label: 'Site map', icon: MapPin },
    { id: 'documents', label: 'Documents', icon: FileText },
    { id: 'upcoming', label: 'Upcoming', icon: CalendarDays },
    { id: NOTIFICATIONS_NAV_ID, label: 'Notifications', icon: Bell, count: unreadNotificationCount },
    { id: 'reports', label: 'Reports', icon: ChartNoAxesCombined },
    { id: 'settings', label: 'Settings', icon: Settings },
  ]
}
