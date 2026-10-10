import { createBrowserRouter, Navigate } from 'react-router-dom'
import { DocumentsPage } from '@/pages/documents/DocumentsPage'
import { HomeRoute } from '@/pages/home/HomeRoute'
import { NotificationsPage } from '@/pages/notifications/NotificationsPage'
import { ReportsPage } from '@/pages/reports/ReportsPage'
import { SettingsPage } from '@/pages/settings/SettingsPage'
import { SignInRoute } from '@/pages/sign-in/SignInRoute'
import { SiteMapPage } from '@/pages/site-map/SiteMapPage'
import { TrackingPage } from '@/pages/tracking/TrackingPage'
import { UpcomingPage } from '@/pages/upcoming/UpcomingPage'
import { AppLayout } from './AppLayout'

export const router = createBrowserRouter([
  { path: '/sign-in', element: <SignInRoute /> },
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomeRoute /> },
      { path: 'tracking', element: <TrackingPage /> },
      { path: 'site-map', element: <SiteMapPage /> },
      { path: 'documents', element: <DocumentsPage /> },
      { path: 'upcoming', element: <UpcomingPage /> },
      { path: 'notifications', element: <NotificationsPage /> },
      { path: 'reports', element: <ReportsPage /> },
      { path: 'settings', element: <SettingsPage /> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
])
