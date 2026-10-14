import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/api/errors'
import { updateUserSettings } from '@/api/users'
import type { CurrentUser } from '@/domain/users'
import { AppStateProvider } from '@/state/AppStateProvider'
import { SettingsPage } from './SettingsPage'

vi.mock('@/api/users', () => ({ updateUserSettings: vi.fn() }))
vi.mock('@/api/notifications', () => ({
  getProjectNotices: vi.fn().mockResolvedValue({ unreadCount: 0, notices: [] }),
  markNotificationRead: vi.fn(),
}))
vi.mock('@/routes/useWorkspace', () => ({
  useWorkspace: () => ({ user: USER, project: USER.projects[0] }),
}))

const USER: CurrentUser = {
  id: 1,
  displayName: 'Dana Reyes',
  initials: 'DR',
  email: 'dana@example.com',
  phone: null,
  roleName: 'Foreman',
  crews: [{ id: 3, name: 'Crew C' }],
  activeProjectId: 7,
  projects: [{ id: 7, name: 'Omaha Fabrication', code: 'OMA', districtName: 'Midwest' }],
  themePreference: 'System',
  emailEnabled: true,
  smsEnabled: false,
}

/** Turns email notices off and presses Save. */
function saveEmailOff() {
  render(
    <AppStateProvider user={USER}>
      <SettingsPage />
    </AppStateProvider>,
  )

  fireEvent.click(screen.getByRole('switch', { name: 'Email notices' }))
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
}

describe('SettingsPage', () => {
  it('saves the changed preference', async () => {
    vi.mocked(updateUserSettings).mockResolvedValue({
      themePreference: 'System',
      emailEnabled: false,
      smsEnabled: false,
    })

    saveEmailOff()

    await waitFor(() => expect(screen.getByRole('button', { name: 'Save' }).hasAttribute('disabled')).toBe(true))
    expect(updateUserSettings).toHaveBeenCalledWith(1, { emailEnabled: false })
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('shows the API message when the save is refused', async () => {
    vi.mocked(updateUserSettings).mockRejectedValue(
      new ApiError(400, 'Choose System, Light, or Dark.', 'invalid_request'),
    )

    saveEmailOff()

    expect((await screen.findByRole('alert')).textContent).toContain('Choose System, Light, or Dark.')
  })

  it('shows the generic message when the save throws', async () => {
    vi.mocked(updateUserSettings).mockRejectedValue(new TypeError('Failed to fetch'))

    saveEmailOff()

    expect((await screen.findByRole('alert')).textContent).toContain('Field Notebook could not save your settings.')
  })
})
