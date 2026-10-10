import SwiftUI
import FactoryKit

struct SettingsView: View {
    let snapshot: HomeSnapshot
    let projects: [HomeProject]
    let selectProject: (HomeProject) -> Void
    let openReports: () -> Void
    let signOut: () -> Void
    @InjectedObservable(\.settingsViewModel) private var viewModel

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                NotebookPageHeader(projectName: snapshot.project.name, title: "Settings",
                                   subtitle: "Make Field Notebook fit your day.")
                LazyVGrid(columns: [GridItem(.adaptive(minimum: 340), spacing: 20, alignment: .top)], alignment: .leading, spacing: 20) {
                    notificationPreferences
                    workspace
                    appearance
                    account
                }
            }
            .padding(28).frame(maxWidth: 1250).frame(maxWidth: .infinity)
        }
        .background(NotebookStyle.bg)
    }

    private var notificationPreferences: some View {
        NotebookSectionCard(title: "Notifications", symbol: "bell") {
            NotebookInfoRow(title: "In-app notices", detail: "Refreshes on Home and when the app returns to the foreground.",
                            symbol: "bell", value: "On")
            Divider()
            NotebookSettingToggle(title: "Email preview", detail: "Preview your email channel preference.",
                                  symbol: "envelope", isOn: $viewModel.emailEnabled)
            Divider()
            NotebookSettingToggle(title: "SMS preview", detail: "Preview your SMS channel preference.",
                                  symbol: "message", isOn: $viewModel.smsEnabled)
            Text("Preferences stay in this demo session. No email or SMS messages are sent.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }

    private var workspace: some View {
        NotebookSectionCard(title: "Your workspace", symbol: "building.2") {
            ProjectSelector(selected: snapshot.project, projects: projects, select: selectProject)
            NotebookInfoRow(title: snapshot.user.crew, detail: snapshot.user.role, symbol: "person.2")
            NotebookInfoRow(title: "Project time", detail: "Central · America/Chicago", symbol: "clock")
            Button(action: openReports) { Label("View reports", systemImage: "chart.bar").frame(minHeight: 44) }
        }
    }

    private var appearance: some View {
        NotebookSectionCard(title: "Appearance", symbol: "circle.lefthalf.filled") {
            NotebookInfoRow(title: "Slate & Teal", detail: "The current app uses the light palette.",
                            symbol: "circle.lefthalf.filled", value: "Light")
        }
    }

    private var account: some View {
        NotebookSectionCard(title: "Your account", symbol: "person.2") {
            NotebookInfoRow(title: snapshot.user.name, detail: "\(snapshot.user.crew) · \(snapshot.user.role)", symbol: "person.2")
            Text("Signed in with the admin demo account.").font(.caption).foregroundStyle(NotebookStyle.muted)
            Button(action: signOut) {
                Label("Sign out", systemImage: "rectangle.portrait.and.arrow.right").frame(minHeight: 44)
            }
        }
    }
}
