import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Plus } from "lucide-react";
import { getProjectTracking } from "@/api/tracking";
import { isAbortError, isApiError } from "@/api/errors";
import { Button } from "@/components/common/Button";
import { PillTabs } from "@/components/common/Tabs";
import { canReportDelay } from "@/domain/permissions";
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
  type TrackingState,
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
  const [reloadCount, setReloadCount] = useState(0);
  const [state, setState] = useState<TrackingState>({ key: null });

  // Scope never widens past the assigned projects. The memo holds one array
  // identity, so a render alone cannot restart the request.
  const projectIds = useMemo(
    () =>
      scope === "all"
        ? user.projects.map((assigned) => assigned.id)
        : [user.activeProjectId],
    [scope, user.projects, user.activeProjectId],
  );
  const requestKey = `${user.id}:${projectIds.join(",")}:${reloadCount}`;

  useEffect(() => {
    const controller = new AbortController();
    const { signal } = controller;

    Promise.all(projectIds.map((id) => getProjectTracking(id, { signal })))
      .then((results) => {
        if (!signal.aborted) {
          setState({
            key: requestKey,
            status: "ready",
            delays: results.flatMap((result) => result.delays.map((row) => row.summary)),
            emptyMessage: results.map((result) => result.projectName + " has no tracking information.").join(" "),
          });
        }
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || signal.aborted) {
          return;
        }

        setState({
          key: requestKey,
          status: "error",
          message: isApiError(error) ? error.message : GENERIC_ERROR,
        });
      });

    // A late answer for the previous scope must not replace the current one.
    return () => controller.abort();
  }, [projectIds, requestKey]);

  const view = toResultsView(state, requestKey, user, filter, search);
  const loaded = toLoadedDelays(state, requestKey);
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
            disabled
            title="Reporting delays is not available yet."
          />
        ) : null}
      </div>

      <TrackingToolbar
        scope={scope}
        search={search}
        isLoading={view.kind === "loading"}
        onScopeChange={setScope}
        onSearchChange={setSearch}
        onRefresh={() => setReloadCount((count) => count + 1)}
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
            onRetry={() => setReloadCount((count) => count + 1)}
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
      />
    </>
  );
}
