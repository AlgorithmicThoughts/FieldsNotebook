import SwiftUI
import FactoryKit

struct TrackingView: View {
    let project: HomeProject
    let crew: String
    var initialFilter: TrackingFilter = .all
    @InjectedObservable(\.trackingViewModel) private var viewModel
    @State private var selectedDelay: DelayRecord?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                NotebookPageHeader(projectName: project.name, title: "Tracking",
                                   subtitle: "One shared list. Every delay, crew, location, and status.")
                TrackingToolbar(viewModel: viewModel)
                NotebookFilterBar(options: TrackingFilter.allCases, selection: $viewModel.filter)
                results
                NotebookRefreshFooter(lastChecked: viewModel.lastChecked, refresh: viewModel.refresh)
            }
            .padding(28).frame(maxWidth: 1250).frame(maxWidth: .infinity)
        }
        .background(NotebookStyle.bg).refreshable { viewModel.refresh() }
        .task(id: project.id) { viewModel.load(project: project, crew: crew, initialFilter: initialFilter) }
        .onChange(of: initialFilter) { _, filter in viewModel.filter = filter }
        .sheet(item: $selectedDelay) { record in
            DelaySummaryView(delay: record.summary, projectName: record.project.name,
                             detail: record.detail, completion: record.completion)
        }
    }

    private var results: some View {
        VStack(spacing: 0) {
            HStack {
                Text("\(viewModel.filteredRecords.count) \(viewModel.filteredRecords.count == 1 ? "delay" : "delays")").font(.headline)
                Spacer()
                Text(viewModel.scope.rawValue).font(.caption).foregroundStyle(NotebookStyle.muted)
            }
            .padding(20)
            if viewModel.filteredRecords.isEmpty {
                ContentUnavailableView {
                    Label("No delays in this view", systemImage: "list.bullet.rectangle")
                } description: {
                    Text("Try another filter or search.")
                } actions: {
                    Button("Clear filters", action: viewModel.clearFilters)
                        .frame(minHeight: 44)
                }
            } else {
                ForEach(viewModel.filteredRecords) { record in
                    Divider().overlay(NotebookStyle.border)
                    Button { selectedDelay = record } label: {
                        DelayRecordRow(record: record)
                    }
                    .buttonStyle(.plain)
                }
            }
        }
        .notebookCard()
    }
}
