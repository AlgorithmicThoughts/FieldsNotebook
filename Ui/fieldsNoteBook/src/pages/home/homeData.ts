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
