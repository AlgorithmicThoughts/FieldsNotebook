import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { requestHandoverCheck, returnHandover, verifyHandover, type DelayActionResult } from '@/api/delayActions'
import type { DelayDetail, DelayStatusCode } from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import { toNextStepNote, toOfferedActions } from '../trackingView'
import { DelayNextStep } from './DelayNextStep'

vi.mock('@/api/delayActions', () => ({
  acknowledgeDelay: vi.fn(),
  reviseCompletionTime: vi.fn(),
  requestHandoverCheck: vi.fn(),
  verifyHandover: vi.fn(),
  returnHandover: vi.fn(),
  reopenDelay: vi.fn(),
  revokeDelay: vi.fn(),
}))
vi.mock('@/lib/exifGps', () => ({ readPhotoGps: vi.fn().mockResolvedValue(null) }))

const PROJECT_ID = 7
const DELAY_ID = 42
const IMPACT_ID = 10

const ACTION_RESULT: DelayActionResult = {
  delayId: DELAY_ID,
  statusCode: 'ReadyForHandover',
  cycleId: 1,
  activityEventId: 900,
  eventType: 'delay.readyForHandover',
  occurredAt: '2026-10-14T15:00:00Z',
  noticeCount: 1,
}

function foreman(id: number, crewName: string): CurrentUser {
  return {
    id,
    displayName: `${crewName} Foreman`,
    initials: 'FF',
    email: `foreman${id}@example.com`,
    phone: null,
    roleName: 'Foreman',
    crews: [{ id, name: crewName }],
    activeProjectId: PROJECT_ID,
    projects: [{ id: PROJECT_ID, name: 'Omaha Fabrication', code: 'OMA', districtName: 'Midwest' }],
    themePreference: 'System',
    emailEnabled: true,
    smsEnabled: false,
  }
}

/** The Foreman of the crew that must remove the blocker. */
const RESPONSIBLE_FOREMAN = foreman(20, 'Steel Fit-Up')
/** The Foreman of the crew the delay blocks. */
const IMPACTED_FOREMAN = foreman(40, 'Welding')

function delayIn(statusCode: DelayStatusCode): DelayDetail {
  return {
    id: DELAY_ID,
    number: 'FN-1042',
    title: 'Rebar delivery missing',
    statusCode,
    tagCode: 'MissingMaterial',
    tagName: 'Missing material',
    ownerCrewName: 'Steel Fit-Up',
    blockedCrewNames: ['Welding'],
    impacts: [{ id: IMPACT_ID, crewName: 'Welding', workItemId: null, workItemTitle: null, verifications: [] }],
    locationName: 'Bay 3',
    projectId: PROJECT_ID,
    projectName: 'Omaha Fabrication',
    isHighPriority: false,
    reportType: 'AfterArrival',
    createdAt: '2026-10-13T14:00:00Z',
    expectedReadyAt: null,
    pin: null,
    siteMapId: null,
    segmentName: null,
    description: 'The slab cannot be poured.',
    reportedBy: { userId: 40, displayName: 'Welding Foreman', crewName: 'Welding', reportedAt: '2026-10-13T14:00:00Z' },
    cycles: [],
    photos: [],
    events: [],
  }
}

/** The card exactly as the delay detail renders it for this user. */
function renderNextStep(delay: DelayDetail, user: CurrentUser) {
  const onCompleted = vi.fn()

  render(
    <DelayNextStep
      note={toNextStepNote(delay, user)}
      offers={toOfferedActions(delay, user)}
      delay={delay}
      user={user}
      onCompleted={onCompleted}
    />,
  )

  return onCompleted
}

/** The confirm button inside the open form, not the offer button with the same words. */
function confirm(label: string) {
  const form = document.querySelector<HTMLElement>('.action-form')
  expect(form).not.toBeNull()
  fireEvent.click(within(form!).getByRole('button', { name: label }))
}

describe('DelayNextStep review flow', () => {
  beforeEach(() => {
    URL.createObjectURL = vi.fn(() => 'blob:proof')
    URL.revokeObjectURL = vi.fn()
  })

  it('lets the responsible crew submit the delay for review with proof of work', async () => {
    vi.mocked(requestHandoverCheck).mockResolvedValue(ACTION_RESULT)
    const onCompleted = renderNextStep(delayIn('Acknowledged'), RESPONSIBLE_FOREMAN)
    const proof = new File(['jpeg'], 'cleared-bay.jpg', { type: 'image/jpeg' })

    fireEvent.click(screen.getByRole('button', { name: 'Submit for review' }))
    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files: [proof] } })
    expect(await screen.findByText('cleared-bay.jpg')).toBeTruthy()
    confirm('Submit for review')

    await waitFor(() => expect(onCompleted).toHaveBeenCalled())
    expect(requestHandoverCheck).toHaveBeenCalledWith(
      PROJECT_ID,
      DELAY_ID,
      [{ file: proof, latitude: null, longitude: null }],
      undefined,
    )
  })

  it('lets the impacted Foreman accept the proof of work for their own crew', async () => {
    vi.mocked(verifyHandover).mockResolvedValue({ ...ACTION_RESULT, statusCode: 'Resolved', eventType: 'delay.resolved' })
    const onCompleted = renderNextStep(delayIn('ReadyForHandover'), IMPACTED_FOREMAN)

    fireEvent.click(screen.getByRole('button', { name: 'Accept for my crew' }))
    confirm('Accept for my crew')

    await waitFor(() => expect(onCompleted).toHaveBeenCalled())
    expect(verifyHandover).toHaveBeenCalledWith(PROJECT_ID, DELAY_ID, { impactId: IMPACT_ID, note: null }, undefined)
  })

  it('lets the impacted Foreman send the work back with what to fix', async () => {
    vi.mocked(returnHandover).mockResolvedValue({ ...ACTION_RESULT, statusCode: 'Acknowledged', eventType: 'delay.handoverReturned' })
    const onCompleted = renderNextStep(delayIn('ReadyForHandover'), IMPACTED_FOREMAN)

    fireEvent.click(screen.getByRole('button', { name: 'Send back' }))
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'Rebar is still on the deck.' } })
    confirm('Send back')

    await waitFor(() => expect(onCompleted).toHaveBeenCalled())
    expect(returnHandover).toHaveBeenCalledWith(
      PROJECT_ID,
      DELAY_ID,
      { impactId: IMPACT_ID, reason: 'Rebar is still on the deck.' },
      undefined,
    )
  })
})
