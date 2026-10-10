import { Monitor, Moon, Sun } from 'lucide-react'
import { Card, CardHeading } from '@/components/common/Card'
import { ChoiceGroup, type Choice } from '@/components/common/ChoiceGroup'
import { Icon } from '@/components/common/Icon'
import type { ThemePreference } from '@/domain/users'
import { SettingError } from './SettingError'

/** The three approved appearance choices. There is no fourth theme. */
const THEME_CHOICES: Choice<ThemePreference>[] = [
  { value: 'System', label: 'System', icon: Monitor },
  { value: 'Light', label: 'Light', icon: Sun },
  { value: 'Dark', label: 'Dark', icon: Moon },
]

interface AppearanceCardProps {
  preference: ThemePreference
  error: string | null
  onSelect: (preference: ThemePreference) => void
}

export function AppearanceCard({ preference, error, onSelect }: AppearanceCardProps) {
  return (
    <Card>
      <CardHeading title="Appearance" action={<Icon icon={Sun} />} />
      <SettingError message={error} />
      <div className="setting-body">
        <p>The same Slate &amp; Teal palette on web, iPhone, and iPad.</p>
        <ChoiceGroup
          label="Appearance"
          value={preference}
          choices={THEME_CHOICES}
          onValueChange={onSelect}
        />
      </div>
    </Card>
  )
}
