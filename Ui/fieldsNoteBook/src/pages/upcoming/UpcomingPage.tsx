import { useState } from "react";
import { CalendarCheck, Megaphone, Plus } from "lucide-react";
import { getProjectUpcoming } from "@/api/upcoming";
import { Badge } from "@/components/common/Badge";
import { Button } from "@/components/common/Button";
import { Card, CardHeading } from "@/components/common/Card";
import { Icon } from "@/components/common/Icon";
import { AddDelayModal } from "@/components/delays/AddDelayModal";
import { EmptyDelays } from "@/components/delays/EmptyDelays";
import { canReportDelay } from "@/domain/permissions";
import { useApiQuery } from "@/hooks/useApiQuery";
import type { BlockedWorkRow as BlockedWork } from "@/domain/upcoming";
import { PROJECT_TIME_LABEL } from "@/lib/projectTime";
import { useWorkspace } from "@/routes/useWorkspace";
import { BlockedWorkRow } from "./components/BlockedWorkRow";
import { UpcomingSkeleton } from "./components/UpcomingSkeleton";
import { nextProjectDayLabel } from "./upcomingView";
import "./UpcomingPage.css";

const GENERIC_ERROR =
  "Field Notebook could not load tomorrow’s work for this project.";

interface BlockedWorkBodyProps {
  rows: BlockedWork[];
}

function BlockedWorkBody({ rows }: BlockedWorkBodyProps) {
  if (rows.length === 0) {
    return (
      <EmptyDelays
        title="No delay blocks tomorrow’s work"
        detail="Every work item planned for the next project day is clear of unresolved blockers right now."
      />
    );
  }

  return (
    <>
      {rows.map((row) => (
        <BlockedWorkRow key={row.id} row={row} />
      ))}
    </>
  );
}

/**
 * Tomorrow's planned work and the unresolved delays that block it, so a crew
 * learns about a blocker before it drives to the site. Every completion time
 * on this page is tentative; time alone never clears a blocker.
 */
export function UpcomingPage() {
  const { user, project } = useWorkspace();
  const projectId = user.activeProjectId;
  const [isReportOpen, setIsReportOpen] = useState(false);
  const { state, reload } = useApiQuery(
    `upcoming:${projectId}`,
    (signal) => getProjectUpcoming(projectId, { signal }),
    GENERIC_ERROR,
  );

  const title = (
    <div className="page-title">
      <div>
        <div className="eyebrow">{project.name}</div>
        <h1>Tomorrow, before arrival.</h1>
        <p>Review delays before your crew reaches the site.</p>
        <p className="upcoming-day">
          {nextProjectDayLabel()} · {PROJECT_TIME_LABEL}
        </p>
      </div>
      {canReportDelay(user) ? (
        <Button
          label="Report delay"
          variant="primary"
          icon={Plus}
          onClick={() => setIsReportOpen(true)}
        />
      ) : null}
    </div>
  );

  const modal = (
    <AddDelayModal
      open={isReportOpen}
      onOpenChange={setIsReportOpen}
      user={user}
      projectId={projectId}
      // Only the API knows whether a new delay blocks work the next project
      // day plans for, so the page asks again instead of guessing a row.
      onCreated={() => reload()}
    />
  );

  if (state.status === "idle" || state.status === "loading") {
    return (
      <>
        {title}
        <Card>
          <CardHeading title="Work affected by a delay" />
          <UpcomingSkeleton />
        </Card>
        {modal}
      </>
    );
  }

  if (state.status === "error") {
    return (
      <>
        {title}
        <Card>
          <div className="upcoming-message" role="alert">
            <h2>Tomorrow’s work is not available</h2>
            <p className="muted">{state.message}</p>
            <Button
              label="Try again"
              variant="primary"
              onClick={() => reload()}
            />
          </div>
        </Card>
        {modal}
      </>
    );
  }

  const rows = state.data;

  return (
    <>
      {title}

      <Card>
        <CardHeading
          title="Work affected by a delay"
          action={
            <Badge
              label={
                rows.length === 1 ? "1 work item" : `${rows.length} work items`
              }
              tone={rows.length === 0 ? "resolved" : "blocked"}
              icon={CalendarCheck}
            />
          }
        />
        <BlockedWorkBody rows={rows} />
      </Card>

      <div className="upcoming-callout">
        <Icon icon={Megaphone} />
        <div>
          <strong>
            The affected crew’s Superintendent receives the arrival notice.
          </strong>
          <p className="small muted">
            They coordinate when the crew arrives. Every completion time here is
            tentative, and a time that has passed does not clear a blocker —
            only the workflow does. Work planned for an earlier day stays listed
            while its delay is unresolved.
          </p>
        </div>
      </div>

      {modal}
    </>
  );
}
