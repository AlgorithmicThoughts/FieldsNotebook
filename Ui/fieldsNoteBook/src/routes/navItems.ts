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
import type { NavItem } from '@/components/layout/Sidebar'

const NOTIFICATIONS_ID = 'notifications'

/** The primary destinations, in sidebar order. The route owns the active state. */
export const NAV_ITEMS: NavItem[] = [
  { id: 'home', path: '/', label: 'Home', icon: House },
  { id: 'tracking', path: '/tracking', label: 'Tracking', icon: ListTodo },
  { id: 'site-map', path: '/site-map', label: 'Site map', icon: MapPin },
  { id: 'documents', path: '/documents', label: 'Documents', icon: FileText },
  { id: 'upcoming', path: '/upcoming', label: 'Upcoming', icon: CalendarDays },
  { id: NOTIFICATIONS_ID, path: '/notifications', label: 'Notifications', icon: Bell },
  { id: 'reports', path: '/reports', label: 'Reports', icon: ChartNoAxesCombined },
  { id: 'settings', path: '/settings', label: 'Settings', icon: Settings },
]

/** The destinations with the live unread total on the notifications item. */
export function navItemsWithUnread(unreadNoticeCount: number): NavItem[] {
  return NAV_ITEMS.map((item) =>
    item.id === NOTIFICATIONS_ID ? { ...item, count: unreadNoticeCount } : item,
  )
}
