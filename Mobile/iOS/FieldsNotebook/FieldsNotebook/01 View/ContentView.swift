import SwiftUI
import FactoryKit

struct ContentView: View {
    @InjectedObservable(\.homeViewModel) private var viewModel
    @State private var selection: NotebookTab = .home
    @State private var moreSelection: NotebookTab?
    @State private var showReportNotice = false
    @State private var trackingMetric: HomeMetric?
    @Environment(\.scenePhase) private var scenePhase

    private var snapshot: HomeSnapshot { viewModel.snapshot }

    var body: some View {
        GeometryReader { geometry in
            VStack(spacing: 0) {
                workspaceHeader(wide: geometry.size.width >= 760)
                Divider().overlay(NotebookStyle.border)
                if geometry.size.width >= 760 {
                    HStack(spacing: 0) {
                        sidebar.frame(width: 230)
                        Divider().overlay(NotebookStyle.border)
                        destination(selection)
                    }
                } else {
                    compactNavigation
                }
            }
            .background(NotebookStyle.bg)
        }
        .foregroundStyle(NotebookStyle.text)
        .tint(NotebookStyle.accent)
        .preferredColorScheme(.light)
        .onChange(of: selection) { _, newValue in
            if newValue == .home { viewModel.refresh() }
        }
        .onChange(of: scenePhase) { _, phase in
            if phase == .active { viewModel.refresh() }
        }
        .alert("Report a delay", isPresented: $showReportNotice) {
            Button("OK", role: .cancel) {}
        } message: {
            Text("Delay reporting will be available in a future update.")
        }
    }

    private func workspaceHeader(wide: Bool) -> some View {
        HStack(spacing: 14) {
            if wide {
                HStack(spacing: 10) {
                    Image(systemName: "arrow.right").font(.headline)
                        .foregroundStyle(NotebookStyle.onAccent)
                        .frame(width: 34, height: 34)
                        .background(NotebookStyle.accent, in: RoundedRectangle(cornerRadius: 10))
                    Text("Field Notebook").font(.headline)
                }
                .frame(width: 206, alignment: .leading)
            }
            projectMenu
            Spacer(minLength: 0)
            Button { navigate(to: .notifications, wide: wide) } label: {
                Image(systemName: snapshot.unreadNotificationCount > 0 ? "bell.badge" : "bell")
                    .font(.title3).frame(width: 44, height: 44)
            }
            .accessibilityLabel("Notifications, \(snapshot.unreadNotificationCount) unread")
            if wide {
                HStack(spacing: 10) {
                    Text(snapshot.user.initials).font(.caption.weight(.bold))
                        .foregroundStyle(NotebookStyle.accent)
                        .frame(width: 36, height: 36).background(NotebookStyle.soft, in: Circle())
                    VStack(alignment: .leading, spacing: 3) {
                        Text(snapshot.user.name).font(.subheadline.weight(.semibold))
                        Text("\(snapshot.user.crew) · \(snapshot.user.role)").font(.caption).foregroundStyle(NotebookStyle.muted)
                    }
                }
            }
        }
        .padding(.horizontal, wide ? 24 : 16).padding(.vertical, 12)
        .background(NotebookStyle.surface)
    }

    private var projectMenu: some View {
        Menu {
            ForEach(viewModel.projects) { project in
                Button {
                    viewModel.selectProject(project)
                    trackingMetric = nil
                } label: {
                    if snapshot.project == project {
                        Label(project.name, systemImage: "checkmark")
                    } else {
                        Text(project.name)
                    }
                }
            }
        } label: {
            HStack(spacing: 10) {
                Image(systemName: "building.2").foregroundStyle(NotebookStyle.accent)
                VStack(alignment: .leading, spacing: 3) {
                    Text(snapshot.project.name).font(.subheadline.weight(.semibold))
                    Text("\(snapshot.project.id) · \(snapshot.project.place)").font(.caption2).foregroundStyle(NotebookStyle.muted)
                }
                Image(systemName: "chevron.up.chevron.down").font(.caption)
            }
            .padding(10).frame(minHeight: 44)
            .background(NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 12))
            .overlay(RoundedRectangle(cornerRadius: 12).stroke(NotebookStyle.border, lineWidth: 1))
        }
        .accessibilityLabel("Change project, \(snapshot.project.name)")
    }

    private var sidebar: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("WORKSPACE").font(.caption2.weight(.bold)).tracking(1.2)
                .foregroundStyle(NotebookStyle.muted).padding(.horizontal, 24).padding(.top, 28).padding(.bottom, 12)
            ScrollView {
                VStack(spacing: 6) {
                    ForEach(NotebookTab.workspaceTabs) { tab in
                        let isSelected = selection == tab
                        let colors = isSelected ? (NotebookStyle.accent, NotebookStyle.soft) : (NotebookStyle.muted, Color.clear)
                        Button { selection = tab } label: {
                            HStack(spacing: 12) {
                                Image(systemName: tab.symbol).frame(width: 22)
                                Text(tab.rawValue).font(.subheadline.weight(.semibold))
                                Spacer(minLength: 0)
                                if tab == .notifications && snapshot.unreadNotificationCount > 0 {
                                    Text(snapshot.unreadNotificationCount, format: .number)
                                        .font(.caption.weight(.semibold)).padding(5)
                                        .background(NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 6))
                                }
                            }
                            .padding(.horizontal, 12).frame(minHeight: 48)
                            .foregroundStyle(colors.0)
                            .background(colors.1, in: RoundedRectangle(cornerRadius: 10))
                            .contentShape(Rectangle())
                        }
                        .buttonStyle(.plain)
                        .accessibilityAddTraits(isSelected ? .isSelected : [])
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

    private var compactNavigation: some View {
        TabView(selection: $selection) {
            ForEach(NotebookTab.compactTabs) { tab in
                destination(tab)
                    .tabItem { Label(tab.rawValue, systemImage: tab.symbol) }
                    .tag(tab)
            }
        }
    }

    @ViewBuilder
    private func destination(_ tab: NotebookTab) -> some View {
        switch tab {
        case .home:
            HomeView(viewModel: viewModel, openTracking: { metric in
                trackingMetric = metric
                selection = .tracking
            }, reportDelay: { showReportNotice = true })
        case .more:
            moreNavigation
        default:
            placeholder(tab)
        }
    }

    private var moreNavigation: some View {
        NavigationStack {
            List(NotebookTab.moreTabs) { tab in
                NavigationLink(value: tab) {
                    Label(tab.rawValue, systemImage: tab.symbol).frame(minHeight: 44)
                }
                .listRowBackground(NotebookStyle.surface)
            }
            .scrollContentBackground(.hidden).background(NotebookStyle.bg)
            .navigationTitle("More")
            .navigationDestination(for: NotebookTab.self) { placeholder($0) }
            .navigationDestination(item: $moreSelection) { placeholder($0) }
        }
    }

    private func placeholder(_ tab: NotebookTab) -> some View {
        ContentUnavailableView {
            Label(tab.rawValue, systemImage: tab.symbol)
        } description: {
            Text(tab == .tracking ? "\(trackingMetric?.rawValue ?? "Delay tracking") will be available in a future update." : "\(tab.rawValue) will be available in a future update.")
        } actions: {
            Button("Back to Home") { selection = .home }.buttonStyle(NotebookPrimaryButton())
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity).background(NotebookStyle.bg)
    }

    private func navigate(to tab: NotebookTab, wide: Bool) {
        if wide {
            selection = tab
        } else {
            moreSelection = tab
            selection = .more
        }
    }

}

#Preview { ContentView() }
