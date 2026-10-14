import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/api/errors'
import { getProjectHome, type ProjectHome } from '@/api/home'
import type { CurrentUser } from '@/domain/users'
import { HomeRoute } from './HomeRoute'

vi.mock('@/api/home', () => ({ getProjectHome: vi.fn() }))
vi.mock('@/components/delays/AddDelayModal', () => ({ AddDelayModal: () => null }))
vi.mock('@/routes/useWorkspace', () => ({ useWorkspace: () => ({ user: USER }) }))

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

const HOME: ProjectHome = {
  metrics: { blockingOurCrews: 4, crewsWaitingOnUs: 0, readyToVerify: 0 },
  attentionDelays: [],
  advanceNotice: null,
  recentActivity: [],
}

function renderHome() {
  return render(
    <MemoryRouter>
      <HomeRoute />
    </MemoryRouter>,
  )
}

describe('HomeRoute', () => {
  it('shows the project home the API returns', async () => {
    vi.mocked(getProjectHome).mockResolvedValue(HOME)

    renderHome()

    expect(await screen.findByText('Your crew, in sync.')).toBeTruthy()
    expect(screen.getByText('4')).toBeTruthy()
    expect(getProjectHome).toHaveBeenCalledWith(7, { signal: expect.any(AbortSignal) })
  })

  it('shows the API message when the request is refused', async () => {
    vi.mocked(getProjectHome).mockRejectedValue(
      new ApiError(403, 'The user has no active access to project 7.', 'forbidden'),
    )

    renderHome()

    expect(await screen.findByRole('alert')).toBeTruthy()
    expect(screen.getByText('The user has no active access to project 7.')).toBeTruthy()
  })

  it('shows the generic message when the request throws', async () => {
    vi.mocked(getProjectHome).mockRejectedValue(new TypeError('Failed to fetch'))

    renderHome()

    expect(await screen.findByText('Field Notebook could not load the delays for this project.')).toBeTruthy()
  })
})
