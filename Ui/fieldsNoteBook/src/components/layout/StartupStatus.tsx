import './layout.css'

interface StartupStatusProps {
  title: string
  detail?: string
}

/** Full-frame message shown before the current user and project are available. */
export function StartupStatus({ title, detail }: StartupStatusProps) {
  return (
    <div className="frame startup-status" role="status">
      <strong>{title}</strong>
      {detail ? <p className="muted">{detail}</p> : null}
    </div>
  )
}
