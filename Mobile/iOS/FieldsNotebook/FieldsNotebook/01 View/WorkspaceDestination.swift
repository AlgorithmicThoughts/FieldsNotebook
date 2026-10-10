import SwiftUI

struct WorkspaceDestination: View {
    let tab: NotebookTab
    let snapshot: HomeSnapshot
    let projects: [HomeProject]
    let selectProject: (HomeProject) -> Void
    let openReports: () -> Void
    let signOut: () -> Void
    let backToHome: () -> Void

    var body: some View {
        switch tab {
        case .settings:
            SettingsView(snapshot: snapshot, projects: projects, selectProject: selectProject,
                         openReports: openReports, signOut: signOut)
        case .reports:
            ReportsView(project: snapshot.project)
        case .notifications:
            NotificationsView(project: snapshot.project)
        default:
            ContentUnavailableView {
                Label(tab.rawValue, systemImage: tab.symbol)
            } description: {
                Text("\(tab.rawValue) will be available in a future update.")
            } actions: {
                Button("Back to Home", action: backToHome).buttonStyle(NotebookPrimaryButton())
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity).background(NotebookStyle.bg)
        }
    }
}
