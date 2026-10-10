import { Bell, Mail, MessageSquare } from 'lucide-react'
import { Badge } from '@/components/common/Badge'
import { Card, CardHeading } from '@/components/common/Card'
import { Icon } from '@/components/common/Icon'
import { Switch } from '@/components/common/Switch'
import { SettingError } from './SettingError'
import type { NotificationChannel } from '../useNotificationChannels'

interface NotificationsCardProps {
  email: string
  /** Users.Phone, which the table allows to be empty. */
  phone: string | null
  emailEnabled: boolean
  smsEnabled: boolean
  error: string | null
  onChannelChange: (channel: NotificationChannel, enabled: boolean) => void
}

export function NotificationsCard({
  email,
  phone,
  emailEnabled,
  smsEnabled,
  error,
  onChannelChange,
}: NotificationsCardProps) {
  return (
    <Card>
      <CardHeading title="Notifications" action={<Icon icon={Bell} />} />
      <SettingError message={error} />

      <div className="setting-row">
        <div>
          <strong>In-app notices</strong>
          <p>
            Field Notebook lists every notice you receive. Opening a notice marks it read, and a
            read grants no workflow permission.
          </p>
        </div>
        <Badge label="On" tone="outline" />
      </div>

      <div className="setting-row">
        <div>
          <strong>
            <Icon icon={Mail} size={16} />
            Email
          </strong>
          <p>Send my notices to {email}.</p>
        </div>
        <Switch
          checked={emailEnabled}
          onCheckedChange={(enabled) => onChannelChange('emailEnabled', enabled)}
          label="Email notices"
        />
      </div>

      <div className="setting-row">
        <div>
          <strong>
            <Icon icon={MessageSquare} size={16} />
            SMS
          </strong>
          <p>{phone === null ? 'No phone number is recorded for you.' : `Text me at ${phone}.`}</p>
        </div>
        <Switch
          checked={smsEnabled}
          onCheckedChange={(enabled) => onChannelChange('smsEnabled', enabled)}
          label="SMS notices"
        />
      </div>

      <p className="setting-note">
        Delivery is a backend concern. Field Notebook honours these preferences once email and SMS
        are implemented.
      </p>
    </Card>
  )
}
