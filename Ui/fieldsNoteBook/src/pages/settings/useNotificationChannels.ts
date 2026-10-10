import { useRef, useState } from 'react'
import { isApiError } from '@/api/errors'
import { updateUserSettings } from '@/api/users'
import type { CurrentUser } from '@/domain/users'

/** UserSettings.EmailEnabled and UserSettings.SmsEnabled. Push is out of scope. */
interface Channels {
  emailEnabled: boolean
  smsEnabled: boolean
}

export type NotificationChannel = keyof Channels

export interface NotificationChannelsState extends Channels {
  /** Message from the last failed save, cleared by the next change. */
  channelError: string | null
  setChannel: (channel: NotificationChannel, enabled: boolean) => void
}

const SAVE_FAILED = 'Field Notebook could not save your notification preferences.'

/**
 * Owns the delivery channel preferences for the signed-in user. Only this page
 * reads them, so they stay local to it rather than joining the shared state.
 */
export function useNotificationChannels(user: CurrentUser): NotificationChannelsState {
  const [channels, setChannels] = useState<Channels>({
    emailEnabled: user.emailEnabled,
    smsEnabled: user.smsEnabled,
  })
  const [channelError, setChannelError] = useState<string | null>(null)
  const latestSave = useRef(0)

  function setChannel(channel: NotificationChannel, enabled: boolean) {
    const save = latestSave.current + 1
    const previous = channels
    const next: Channels = { ...previous, [channel]: enabled }

    latestSave.current = save
    setChannels(next)
    setChannelError(null)

    updateUserSettings(user.id, next).catch((error: unknown) => {
      // A later change already owns the switches, so this reply is obsolete.
      if (latestSave.current !== save) {
        return
      }

      setChannels(previous)
      setChannelError(isApiError(error) ? error.message : SAVE_FAILED)
    })
  }

  return { ...channels, channelError, setChannel }
}
