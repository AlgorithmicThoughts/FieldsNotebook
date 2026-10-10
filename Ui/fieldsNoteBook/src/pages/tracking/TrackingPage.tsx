import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Plus } from "lucide-react";
import { getDelaysByProjectId } from "@/api/delays";
import { isAbortError, isApiError } from "@/api/errors";
import { Button } from "@/components/common/Button";
import { PillTabs } from "@/components/common/Tabs";
import { AddDelayModal } from "@/components/delays/AddDelayModal";
import type { DelaySummary } from "@/domain/delays";
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
  const [isReportOpen, setIsReportOpen] = useState(false);
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
  const requestKey = `${projectIds.join(",")}:${reloadCount}`;

  useEffect(() => {
    const controller = new AbortController();
    const { signal } = controller;

    Promise.all(projectIds.map((id) => getDelaysByProjectId(id, { signal })))
      .then((results) => {
        if (!signal.aborted) {
          setState({
            key: requestKey,
            status: "ready",
            delays: results.flat(),
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

  /** A delay the Foreman just raised joins the list without another request. */
  function addDelay(delay: DelaySummary) {
    setState((current) =>
      current.key === null || current.status === "error"
        ? current
        : { ...current, delays: [delay, ...current.delays] },
    );
  }

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

      <AddDelayModal
        open={isReportOpen}
        onOpenChange={setIsReportOpen}
        user={user}
        projectId={user.activeProjectId}
        onCreated={addDelay}
      />
    </>
  );
}
