import { Badge } from '@/components/common/Badge'
import { DELAY_STATUS_DISPLAY, type DelayStatusCode } from '@/domain/delays'

interface ScheduleRow {
  time: string
  crew: string
  activity: string
  statusCode: DelayStatusCode
}

/** Sample rows. A schedule is not a contract and no table stores these. */
const ROWS: ScheduleRow[] = [
  { time: '07:00 AM', crew: 'Crew B', activity: 'Frame 12 weld', statusCode: 'Raised' },
  {
    time: '02:00 PM',
    crew: 'Crew A',
    activity: 'Tentative fit-up handover',
    statusCode: 'Acknowledged',
  },
]

/** The sample day plan. A tentative time never clears a blocker on its own. */
export function SchedulePreview() {
  return (
    <>
      <h3>Thursday, 8 October</h3>

      <div className="mt-3">
        {ROWS.map((row) => {
          const display = DELAY_STATUS_DISPLAY[row.statusCode]

          return (
            <div key={row.time} className="sample-doc-row">
              <span>
                {row.time} · {row.crew} · {row.activity}
              </span>
              <Badge label={display.label} tone={display.tone} icon={display.icon} />
            </div>
          )
        })}
      </div>

      <p className="small muted mt-[18px]">
        This sample schedule grants no clearance to start work. The affected crew's Foreman
        verifies the handover before the delay resolves.
      </p>
    </>
  )
}
