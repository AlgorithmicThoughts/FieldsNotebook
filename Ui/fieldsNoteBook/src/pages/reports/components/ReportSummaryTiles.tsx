import type { ReportSummary, ReportTagCount } from "../reportsView";

interface TagBarProps {
  tag: ReportTagCount;
  /** Delays in scope, so each bar reads as a share of the whole report. */
  total: number;
}

/**
 * A reason nobody raised still gets a row. It reads as empty through its count,
 * its lighter name, and a dashed instead of a filled track, so the difference
 * never rests on colour.
 */
function TagBar({ tag, total }: TagBarProps) {
  const width = total === 0 ? 0 : (tag.count / total) * 100;

  return (
    <div
      className="report-tag"
      data-empty={tag.count === 0 ? "true" : undefined}
    >
      <div className="report-tag-row">
        <strong>{tag.name}</strong>
        <span className="report-tag-count">{tag.count}</span>
      </div>
      <span className="report-tag-bar" aria-hidden="true">
        <span style={{ width: `${width}%` }} />
      </span>
    </div>
  );
}

interface ReportSummaryTilesProps {
  summary: ReportSummary;
}

/** Counted from the rows on screen. No estimate, trend, or forecast. */
export function ReportSummaryTiles({ summary }: ReportSummaryTilesProps) {
  return (
    <div className="report-summary">
      <div>
        <h3>Reasons</h3>
        <p className="report-scope-hint">
          Every approved reason tag, counted from the delays in scope.
        </p>
        <div className="report-tags">
          {summary.tagCounts.map((tag) => (
            <TagBar key={tag.name} tag={tag} total={summary.total} />
          ))}
        </div>
      </div>
    </div>
  );
}
