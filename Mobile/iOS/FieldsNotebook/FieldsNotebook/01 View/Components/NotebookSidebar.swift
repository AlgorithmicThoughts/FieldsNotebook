import SwiftUI

struct NotebookSidebar: View {
    let snapshot: HomeSnapshot
    let unreadCount: Int
    @Binding var selection: NotebookTab

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("WORKSPACE").font(.caption2.weight(.bold)).tracking(1.2)
                .foregroundStyle(NotebookStyle.muted)
                .padding(.horizontal, 24).padding(.top, 28).padding(.bottom, 12)
            ScrollView {
                VStack(spacing: 6) {
                    ForEach(NotebookTab.workspaceTabs) { tab in
                        NotebookSidebarItem(tab: tab, selected: selection == tab,
                                            unreadCount: unreadCount) { selection = tab }
                    }
                }
                .padding(.horizontal, 12)
            }
            VStack(alignment: .leading, spacing: 7) {
                Label(snapshot.user.crew, systemImage: "person.2").font(.subheadline.weight(.semibold))
                Text(snapshot.user.role).font(.caption)
                Text("Project time · Central").font(.caption)
            }
            .foregroundStyle(NotebookStyle.muted).padding(24)
        }
        .background(NotebookStyle.surface)
    }
}
