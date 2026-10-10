import SwiftUI

struct WorkspaceHeader: View {
    let snapshot: HomeSnapshot
    let projects: [HomeProject]
    let wide: Bool
    let selectProject: (HomeProject) -> Void
    let openNotifications: () -> Void
    let signOut: () -> Void

    var body: some View {
        HStack(spacing: 14) {
            if wide { NotebookBrand().frame(width: 206, alignment: .leading) }
            ProjectSelector(selected: snapshot.project, projects: projects, select: selectProject)
            Spacer(minLength: 0)
            Button(action: openNotifications) {
                Image(systemName: snapshot.unreadNotificationCount > 0 ? "bell.badge" : "bell")
                    .font(.title3).frame(width: 44, height: 44)
            }
            .accessibilityLabel("Notifications, \(snapshot.unreadNotificationCount) unread")
            WorkspaceAccountMenu(user: snapshot.user, wide: wide, signOut: signOut)
        }
        .padding(.horizontal, wide ? 24 : 16).padding(.vertical, 12)
        .background(NotebookStyle.surface)
    }
}
