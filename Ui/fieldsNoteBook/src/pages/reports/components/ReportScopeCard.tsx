import { Card } from '@/components/common/Card'
import { CheckboxRow } from '@/components/common/Checkbox'
import { FormField, FormFieldset } from '@/components/common/Field'
import { TextInput } from '@/components/common/Input'
import type { UserProject } from '@/domain/users'

interface ReportScopeCardProps {
  /** Only the projects the user is assigned to may be reported on. */
  projects: UserProject[]
  selectedProjectIds: number[]
  onSelectedProjectIdsChange: (next: number[]) => void
  dateFrom: string
  dateTo: string
  onDateFromChange: (next: string) => void
  onDateToChange: (next: string) => void
}

/** The whole report request: the projects, and the two optional date bounds. */
export function ReportScopeCard({
  projects,
  selectedProjectIds,
  onSelectedProjectIdsChange,
  dateFrom,
  dateTo,
  onDateFromChange,
  onDateToChange,
}: ReportScopeCardProps) {
  /** Rebuilt from the project list so the selection keeps a stable order. */
  function toggleProject(projectId: number, checked: boolean) {
    onSelectedProjectIdsChange(
      projects
        .filter((project) =>
          project.id === projectId ? checked : selectedProjectIds.includes(project.id),
        )
        .map((project) => project.id),
    )
  }

  return (
    <Card>
      <div className="report-scope">
        <div>
          <h2>Report scope</h2>
          <p className="report-scope-hint">Choose one or more projects.</p>
        </div>

        <FormFieldset
          name="report-projects"
          legend="Projects"
          description="Only the projects you are assigned to."
        >
          <div className="report-projects">
            {projects.map((project) => (
              <CheckboxRow
                key={project.id}
                checked={selectedProjectIds.includes(project.id)}
                onCheckedChange={(checked) => toggleProject(project.id, checked)}
                label={project.name}
                detail={project.code}
              />
            ))}
          </div>
        </FormFieldset>

        <div className="stack">
          <FormField name="date-from" label="Date from (optional)">
            <TextInput
              type="date"
              value={dateFrom}
              onChange={(event) => onDateFromChange(event.target.value)}
            />
          </FormField>

          <FormField name="date-to" label="Date to (optional)">
            <TextInput
              type="date"
              value={dateTo}
              onChange={(event) => onDateToChange(event.target.value)}
            />
          </FormField>
        </div>

        <p className="report-scope-hint">
          The report request carries only these projects and these dates. The preview filters
          nothing else.
        </p>
      </div>
    </Card>
  )
}
