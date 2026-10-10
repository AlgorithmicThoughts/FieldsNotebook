import './routes.css'

interface PagePlaceholderProps {
  title: string
  /** Work item that replaces this stub. */
  workItem: string
}

/** Temporary page body so navigation works before the page is built. */
export function PagePlaceholder({ title, workItem }: PagePlaceholderProps) {
  return (
    <div className="page-placeholder">
      <h1>{title}</h1>
      <p>This page is not built yet. {workItem} delivers it.</p>
    </div>
  )
}
