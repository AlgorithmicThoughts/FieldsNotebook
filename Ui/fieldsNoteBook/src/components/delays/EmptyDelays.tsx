import './delays.css'

interface EmptyDelaysProps {
  title: string
  detail: string
}

export function EmptyDelays({ title, detail }: EmptyDelaysProps) {
  return (
    <div className="empty">
      <strong>{title}</strong>
      <p className="small">{detail}</p>
    </div>
  )
}
