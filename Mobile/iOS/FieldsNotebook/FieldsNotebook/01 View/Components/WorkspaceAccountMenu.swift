import SwiftUI

struct WorkspaceAccountMenu: View {
    let user: HomeUser
    let wide: Bool
    let signOut: () -> Void

    var body: some View {
        Menu {
            Text("\(user.name) · \(user.role)")
            Button("Sign out", systemImage: "rectangle.portrait.and.arrow.right", action: signOut)
        } label: {
            HStack(spacing: 10) {
                Text(user.initials).font(.caption.weight(.bold))
                    .foregroundStyle(NotebookStyle.accent)
                    .frame(width: 36, height: 36).background(NotebookStyle.soft, in: Circle())
                if wide {
                    VStack(alignment: .leading, spacing: 3) {
                        Text(user.name).font(.subheadline.weight(.semibold))
                        Text("\(user.crew) · \(user.role)").font(.caption).foregroundStyle(NotebookStyle.muted)
                    }
                    Image(systemName: "chevron.down").font(.caption2)
                }
            }
            .frame(minHeight: 44)
        }
        .accessibilityLabel("Account, \(user.name)")
    }
}
