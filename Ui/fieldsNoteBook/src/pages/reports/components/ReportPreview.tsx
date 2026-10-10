import { Building2, CalendarDays, TriangleAlert } from "lucide-react";
import { Button } from "@/components/common/Button";
import { Card, CardHeading } from "@/components/common/Card";
import type { DelayReportRow } from "@/domain/reports";
import { toReportSummary, type PreviewView } from "../reportsView";
import { ReportEmpty } from "./ReportEmpty";
import { ReportSkeleton } from "./ReportSkeleton";
import { ReportSummaryTiles } from "./ReportSummaryTiles";

interface ReadyPreviewProps {
  rows: DelayReportRow[];
  /** DelayTags.Name in lookup order, so an unused reason still counts as zero. */
  tagNames: string[];
}

function ReadyPreview({ rows, tagNames }: ReadyPreviewProps) {
  if (rows.length === 0) {
    return (
      <ReportEmpty
        icon={CalendarDays}
        title="No delays in this scope"
        detail="The selected projects recorded nothing between these dates. Widen the range or add a project."
      />
    );
  }

  return <ReportSummaryTiles summary={toReportSummary(rows, tagNames)} />;
}

interface PreviewBodyProps {
  view: PreviewView;
  onRetry: () => void;
}

function PreviewBody({ view, onRetry }: PreviewBodyProps) {
  if (view.kind === "no-scope") {
    return (
      <ReportEmpty
        icon={Building2}
        title="Choose at least one project"
        detail="A report covers the projects you select. Pick one to see its delays."
      />
    );
  }

  if (view.kind === "loading") {
    return <ReportSkeleton />;
  }

  if (view.kind === "error") {
    return (
      <div role="alert">
        <ReportEmpty
          icon={TriangleAlert}
          title="The report is not available"
          detail={view.message}
          action={
            <Button label="Try again" variant="primary" onClick={onRetry} />
          }
        />
      </div>
    );
  }

  return <ReadyPreview rows={view.rows} tagNames={view.tagNames} />;
}

interface ReportPreviewProps {
  view: PreviewView;
  onRetry: () => void;
}

/** The matching delays with a count, their summary tiles, and the rows. */
export function ReportPreview({ view, onRetry }: ReportPreviewProps) {
  return (
    <Card>
      <CardHeading
        title={view.kind === "ready" ? `${view.rows.length} Issue Records` : ""}
      ></CardHeading>
      {}
      <PreviewBody view={view} onRetry={onRetry} />
    </Card>
  );
}
