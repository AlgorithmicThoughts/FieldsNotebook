import SwiftUI

struct FeatureDestination: View {
    let tab: NotebookTab
    let snapshot: HomeSnapshot
    let trackingMetric: HomeMetric?
    let projects: [HomeProject]
    let selectProject: (HomeProject) -> Void
    let openReports: () -> Void
    let signOut: () -> Void
    let backToHome: () -> Void

    var body: some View {
        switch tab {
        case .tracking:
            TrackingView(project: snapshot.project, crew: snapshot.user.crew,
                         initialFilter: TrackingFilter(metric: trackingMetric))
        case .documents:
            DocumentsView(project: snapshot.project)
        case .upcoming:
            UpcomingView(project: snapshot.project)
        default:
            WorkspaceDestination(tab: tab, snapshot: snapshot, projects: projects, selectProject: selectProject,
                                 openReports: openReports, signOut: signOut, backToHome: backToHome)
        }
    }
}
