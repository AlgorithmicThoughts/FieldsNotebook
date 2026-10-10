import { Check } from 'lucide-react'
import { Icon } from '@/components/common/Icon'
import type { HandoverStep } from '../trackingView'

/** The circle alone carries the state, so each step also says it in text. */
const STATE_NOTE: Record<HandoverStep['state'], string> = {
  done: 'Done.',
  current: 'In progress.',
  pending: 'Not started.',
}

interface DelayHandoverProgressProps {
  /** Earliest stage first. A reopen starts the stages again. */
  steps: HandoverStep[]
}

/** The handover stages of the current cycle, and the crew each one waits on. */
export function DelayHandoverProgress({ steps }: DelayHandoverProgressProps) {
  return (
    <section>
      <h3>Handover progress</h3>

      <ol className="step-list">
        {steps.map((step, index) => (
          <li key={step.id} className={`step ${step.state}`}>
            <span className="step-number" aria-hidden="true">
              {step.state === 'done' ? <Icon icon={Check} size={14} /> : index + 1}
            </span>
            <div>
              <strong>{step.label}</strong>
              <span className="sr-only"> {STATE_NOTE[step.state]}</span>
              <p>{step.actor}</p>
            </div>
          </li>
        ))}
      </ol>
    </section>
  )
}
