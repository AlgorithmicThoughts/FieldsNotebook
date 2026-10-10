import SwiftUI
import FactoryKit

struct ContentView: View {
    var onSignOut: () -> Void = {}
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
                WorkspaceHeader(snapshot: snapshot, projects: viewModel.projects, wide: geometry.size.width >= 760,
                                selectProject: selectProject,
                                openNotifications: { navigate(to: .notifications, wide: geometry.size.width >= 760) },
                                signOut: onSignOut)
                Divider().overlay(NotebookStyle.border)
                if geometry.size.width >= 760 {
                    HStack(spacing: 0) {
                        NotebookSidebar(snapshot: snapshot, selection: $selection).frame(width: 230)
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

    private func selectProject(_ project: HomeProject) {
        viewModel.selectProject(project)
        trackingMetric = nil
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
            feature(tab)
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
            .navigationDestination(for: NotebookTab.self) { feature($0) }
            .navigationDestination(item: $moreSelection) { feature($0) }
        }
    }

    private func feature(_ tab: NotebookTab) -> some View {
        FeatureDestination(tab: tab, snapshot: snapshot, trackingMetric: trackingMetric) { selection = .home }
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
