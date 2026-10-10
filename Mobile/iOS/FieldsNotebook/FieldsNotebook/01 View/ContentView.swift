import SwiftUI
import FactoryKit

struct ContentView: View {
    var onSignOut: () -> Void = {}
    @InjectedObservable(\.homeViewModel) private var viewModel
    @InjectedObservable(\.notificationsViewModel) private var notifications
    @State private var selection: NotebookTab = .home
    @State private var moreSelection: NotebookTab?
    @State private var showReportBlocker = false
    @State private var trackingMetric: HomeMetric?
    @Environment(\.scenePhase) private var scenePhase

    private var snapshot: HomeSnapshot { viewModel.snapshot }

    var body: some View {
        GeometryReader { geometry in
            VStack(spacing: 0) {
                WorkspaceHeader(snapshot: snapshot, projects: viewModel.projects, wide: geometry.size.width >= 760,
                                unreadCount: notifications.unreadCount,
                                selectProject: selectProject,
                                openNotifications: { navigate(to: .notifications, wide: geometry.size.width >= 760) },
                                signOut: onSignOut)
                Divider().overlay(NotebookStyle.border)
                if geometry.size.width >= 760 {
                    HStack(spacing: 0) {
                        NotebookSidebar(snapshot: snapshot, unreadCount: notifications.unreadCount, selection: $selection).frame(width: 230)
                        Divider().overlay(NotebookStyle.border)
                        destination(selection, wide: true)
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
        .task(id: snapshot.project.id) { notifications.load(project: snapshot.project) }
        .onChange(of: selection) { _, newValue in
            if newValue == .home { refreshWorkspace() }
        }
        .onChange(of: scenePhase) { _, phase in
            if phase == .active { refreshWorkspace() }
        }
        .sheet(isPresented: $showReportBlocker) {
            ReportBlockerSheet(project: snapshot.project, user: snapshot.user, onSubmitted: refreshWorkspace) {
                trackingMetric = nil
                selection = .tracking
            }
        }
    }

    private func selectProject(_ project: HomeProject) {
        viewModel.selectProject(project)
        trackingMetric = nil
    }

    private func refreshWorkspace() {
        viewModel.refresh()
        notifications.refresh()
    }

    private var compactNavigation: some View {
        TabView(selection: $selection) {
            ForEach(NotebookTab.compactTabs) { tab in
                destination(tab, wide: false)
                    .tabItem { Label(tab.rawValue, systemImage: tab.symbol) }
                    .tag(tab)
            }
        }
    }

    @ViewBuilder
    private func destination(_ tab: NotebookTab, wide: Bool) -> some View {
        switch tab {
        case .home:
            HomeView(viewModel: viewModel, openTracking: { metric in
                trackingMetric = metric
                selection = .tracking
            }, reportDelay: { showReportBlocker = true })
        case .more:
            moreNavigation
        default:
            feature(tab, wide: wide)
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
            .navigationDestination(for: NotebookTab.self) { feature($0, wide: false) }
            .navigationDestination(item: $moreSelection) { feature($0, wide: false) }
        }
    }

    private func feature(_ tab: NotebookTab, wide: Bool) -> some View {
        FeatureDestination(tab: tab, snapshot: snapshot, trackingMetric: trackingMetric,
                           projects: viewModel.projects, selectProject: selectProject,
                           openReports: { navigate(to: .reports, wide: wide) }, signOut: onSignOut) { selection = .home }
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
