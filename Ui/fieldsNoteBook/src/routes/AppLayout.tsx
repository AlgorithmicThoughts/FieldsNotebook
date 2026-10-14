import { useEffect, useRef, useState } from "react";
import { Navigate, Outlet, useLocation, useNavigate } from "react-router-dom";
import { errorMessage, isAbortError, isApiError } from "@/api/errors";
import { getUser, getUserOnProject } from "@/api/users";
import { Button } from "@/components/common/Button";
import { AppShell } from "@/components/layout/AppShell";
import { Sidebar } from "@/components/layout/Sidebar";
import { TopBar } from "@/components/layout/TopBar";
import { Skeleton } from "@/components/ui/skeleton";
import type { ProjectSummary } from "@/domain/projects";
import { crewLabel, type CurrentUser } from "@/domain/users";
import { PROJECT_TIME_LABEL } from "@/lib/projectTime";
import { AppStateProvider } from "@/state/AppStateProvider";
import { useAppState } from "@/state/appState";
import { useAuth } from "@/state/auth";
import { navItemsWithUnread } from "./navItems";
import type { WorkspaceContext } from "./useWorkspace";
import "./routes.css";

/** `attempt` records which load produced the result, so a retry supersedes it. */
type SessionState =
  | { attempt: null }
  | { attempt: number; status: "error"; message: string }
  | {
      attempt: number;
      status: "ready";
      user: CurrentUser;
      project: ProjectSummary;
    };

function activeProjectOf(user: CurrentUser): ProjectSummary {
  const project = user.projects.find(
    (candidate) => candidate.id === user.activeProjectId,
  );

  if (project === undefined) {
    throw new Error("The active project is missing from the signed-in user.");
  }

  return project;
}

function messageOf(error: unknown): string {
  return isApiError(error)
    ? error.message
    : "Field Notebook could not load your workspace.";
}

const SWITCH_FAILED =
  "Field Notebook could not open that project. You are still on this one.";

/** The project switch state the shell shows while it runs, and after it fails. */
interface ProjectSwitch {
  isSwitching: boolean;
  error: string | null;
  change: (projectId: number) => void;
}

/** The shell every page renders inside. Nothing loads until a user is signed in. */
export function AppLayout() {
  const { session } = useAuth();

  if (session === null) {
    return <Navigate to="/sign-in" replace />;
  }

  return (
    <WorkspaceLoader key={session.user.userId} userId={session.user.userId} />
  );
}

/** Loads the signed-in user and their active project. */
function WorkspaceLoader({ userId }: { userId: number }) {
  const [session, setSession] = useState<SessionState>({ attempt: null });
  const [attempt, setAttempt] = useState(0);
  const [isSwitchingProject, setIsSwitchingProject] = useState(false);
  const [switchError, setSwitchError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    // One request: /api/me already carries each project's code and district.
    const load = async () => {
      const user = await getUser(userId, { signal: controller.signal });

      return { user, project: activeProjectOf(user) };
    };

    load()
      .then(({ user, project }) => {
        if (controller.signal.aborted) {
          return;
        }

        setSession({ attempt, status: "ready", user, project });
      })
      .catch((error: unknown) => {
        if (isAbortError(error) || controller.signal.aborted) {
          return;
        }

        setSession({ attempt, status: "error", message: messageOf(error) });
      });

    return () => controller.abort();
  }, [attempt, userId]);

  /**
   * Moves the workspace to another assigned project. The role, the crew, and
   * the project all change together, so the shell waits for the whole answer
   * and a failed switch leaves the current project exactly as it was.
   */
  function changeProject(projectId: number) {
    setIsSwitchingProject(true);
    setSwitchError(null);

    getUserOnProject(userId, projectId)
      .then((user) =>
        setSession({
          attempt,
          status: "ready",
          user,
          project: activeProjectOf(user),
        }),
      )
      .catch((error: unknown) => {
        setSwitchError(errorMessage(error, SWITCH_FAILED));
      })
      .finally(() => setIsSwitchingProject(false));
  }

  const projectSwitch: ProjectSwitch = {
    isSwitching: isSwitchingProject,
    error: switchError,
    change: changeProject,
  };

  if (session.attempt !== attempt) {
    return <WorkspaceSkeleton />;
  }

  if (session.status === "error") {
    return (
      <div className="workspace-message" role="alert">
        <h1>Field Notebook is not available</h1>
        <p>{session.message}</p>
        <Button
          label="Try again"
          variant="primary"
          onClick={() => setAttempt((n) => n + 1)}
        />
      </div>
    );
  }

  return (
    <Workspace
      user={session.user}
      project={session.project}
      projectSwitch={projectSwitch}
    />
  );
}

interface WorkspaceProps {
  user: CurrentUser;
  project: ProjectSummary;
  projectSwitch: ProjectSwitch;
}

/** Mounted once the user is known, so shared state starts from saved values. */
function Workspace({ user, project, projectSwitch }: WorkspaceProps) {
  return (
    <AppStateProvider user={user}>
      <WorkspaceShell
        user={user}
        project={project}
        projectSwitch={projectSwitch}
      />
    </AppStateProvider>
  );
}

/** The shell itself, reading the shared state the Settings page also writes. */
function WorkspaceShell({ user, project, projectSwitch }: WorkspaceProps) {
  const { theme, selectThemePreference, unreadNoticeCount } = useAppState();
  const { signOut } = useAuth();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const focusedPathRef = useRef(pathname);

  // Keyboard users land in the new page rather than back in the sidebar. Comparing
  // paths, not a first-render flag, keeps StrictMode's repeated effect from focusing
  // the page on load, and preventScroll keeps the top bar in view.
  useEffect(() => {
    if (focusedPathRef.current === pathname) {
      return;
    }

    focusedPathRef.current = pathname;
    document.getElementById("main")?.focus({ preventScroll: true });
  }, [pathname]);

  const context: WorkspaceContext = { user, project };

  return (
    <AppShell
      header={
        <TopBar
          projectName={project.name}
          projectCode={project.code}
          projectPlace={project.districtName ?? "District not recorded"}
          projects={user.projects}
          activeProjectId={user.activeProjectId}
          isSwitchingProject={projectSwitch.isSwitching}
          userDisplayName={user.displayName}
          userInitials={user.initials}
          userRoleName={user.roleName}
          userCrewName={crewLabel(user)}
          theme={theme}
          hasUnreadNotifications={unreadNoticeCount > 0}
          onThemeToggle={() =>
            selectThemePreference(theme === "dark" ? "Light" : "Dark")
          }
          onProjectChange={projectSwitch.change}
          onNotificationsOpen={() => void navigate("/notifications")}
          onSignOut={signOut}
        />
      }
      sidebar={
        <Sidebar
          items={navItemsWithUnread(unreadNoticeCount)}
          crewName={crewLabel(user)}
          roleName={user.roleName}
          projectTimeLabel={PROJECT_TIME_LABEL}
        />
      }
    >
      {projectSwitch.error ? (
        <p className="project-switch-error" role="alert">
          {projectSwitch.error}
        </p>
      ) : null}

      <Outlet context={context} />
    </AppShell>
  );
}

function WorkspaceSkeleton() {
  return (
    <div className="workspace-message" aria-busy="true">
      <Skeleton className="h-[86px] w-full" />
      <Skeleton className="h-8 w-72" />
      <Skeleton className="h-40 w-full" />
    </div>
  );
}
