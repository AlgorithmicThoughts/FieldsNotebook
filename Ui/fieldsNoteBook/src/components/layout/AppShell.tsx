import type { ReactNode } from 'react'
import './layout.css'

interface AppShellProps {
  header: ReactNode
  sidebar: ReactNode
  children: ReactNode
}

/** Desktop frame: top bar, fixed sidebar, and the workspace for the current page. */
export function AppShell({ header, sidebar, children }: AppShellProps) {
  return (
    <div className="frame">
      {header}
      <div className="shell">
        {sidebar}
        <main className="workspace" id="main" tabIndex={-1}>
          {children}
        </main>
      </div>
    </div>
  )
}
