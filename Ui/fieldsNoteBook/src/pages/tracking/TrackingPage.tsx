import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Plus } from "lucide-react";
import { getProjectTracking } from "@/api/tracking";
import { Button } from "@/components/common/Button";
import { AddDelayModal } from "@/components/delays/AddDelayModal";
import { PillTabs } from "@/components/common/Tabs";
import { canReportDelay } from "@/domain/permissions";
import { useApiQuery } from "@/hooks/useApiQuery";
import { useWorkspace } from "@/routes/useWorkspace";
import { DelayDetailModal } from "./components/DelayDetailModal";
import { TrackingResults } from "./components/TrackingResults";
import { TrackingToolbar } from "./components/TrackingToolbar";
import {
  TRACKING_FILTERS,
  toLoadedDelays,
  toResultsView,
  type TrackingFilter,
  type TrackingScope,
  type TrackingData,
} from "./trackingView";
import "./TrackingPage.css";

const GENERIC_ERROR =
  "Field Notebook could not load the delays for this scope.";

/** The URL carries the open delay, so Home can link straight to one. */
const DELAY_PARAM = "delay";

/**
 * Every delay the user is authorised for, with scope, search, status filters,
 * and a detail view. The request never reaches past `user.projects`.
 */
export function TrackingPage() {
  const { user, project } = useWorkspace();
  const [searchParams, setSearchParams] = useSearchParams();
  const [scope, setScope] = useState<TrackingScope>("project");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<TrackingFilter>("all");
  const [isReportOpen, setIsReportOpen] = useState(false);

  // Scope never widens past the assigned projects.
  const projectIds = useMemo(
    () =>
      scope === "all"
        ? user.projects.map((assigned) => assigned.id)
        : [user.activeProjectId],
    [scope, user.projects, user.activeProjectId],
  );
  // A new scope aborts the previous request, so a late answer never replaces the current one.
  const { state, reload } = useApiQuery(
    `tracking:${user.id}:${projectIds.join(",")}`,
    async (signal): Promise<TrackingData> => {
      const results = await Promise.all(projectIds.map((id) => getProjectTracking(id, { signal })));

      return {
        delays: results.flatMap((result) => result.delays.map((row) => row.summary)),
        emptyMessage: results.map((result) => result.projectName + " has no tracking information.").join(" "),
      };
    },
    GENERIC_ERROR,
  );

  const view = toResultsView(state, user, filter, search);
  const loaded = toLoadedDelays(state);
  const openedNumber = searchParams.get(DELAY_PARAM);
  const openedDelay =
    loaded.find((delay) => delay.number === openedNumber) ?? null;

  return (
    <>
      <div className="page-title">
        <div>
          <div className="eyebrow">{project.name}</div>
          <h1>Tracking</h1>
          <p>
            One shared list. See the issue, its crew, its location, and its
            status.
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

      <TrackingToolbar
        scope={scope}
        search={search}
        isLoading={view.kind === "loading"}
        onScopeChange={setScope}
        onSearchChange={setSearch}
        onRefresh={reload}
      />

      <div className="tracking-filters">
        <PillTabs
          label="Filter the delays"
          value={filter}
          onValueChange={setFilter}
          options={TRACKING_FILTERS}
        >
          <TrackingResults
            view={view}
            onRetry={reload}
            onOpenDelay={(delayNumber) =>
              setSearchParams({ [DELAY_PARAM]: delayNumber })
            }
          />
        </PillTabs>
      </div>

      <DelayDetailModal
        summary={openedDelay}
        user={user}
        onClose={() => setSearchParams({}, { replace: true })}
        onDelayChanged={reload}
      />
      <AddDelayModal
        open={isReportOpen}
        onOpenChange={setIsReportOpen}
        user={user}
        projectId={user.activeProjectId}
        onCreated={reload}
      />
    </>
  );
}
