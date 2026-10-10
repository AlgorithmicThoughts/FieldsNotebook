import SwiftUI
import FactoryKit

struct UpcomingView: View {
    let project: HomeProject
    @InjectedObservable(\.upcomingViewModel) private var viewModel
    @State private var selectedDelay: DelayRecord?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                NotebookPageHeader(projectName: project.name, title: "Tomorrow, before arrival.",
                                   subtitle: "Review delays before your crew reaches the site.")
                Label("Sample · \(viewModel.work.first?.day ?? "Upcoming schedule") · Project time: Central", systemImage: "calendar")
                    .font(.subheadline.weight(.semibold)).foregroundStyle(NotebookStyle.muted)
                if viewModel.work.isEmpty {
                    ContentUnavailableView("No upcoming delays reported", systemImage: "calendar",
                                           description: Text("This does not confirm that all work is ready."))
                }
                ForEach(viewModel.work) { work in
                    UpcomingWorkCard(work: work) { selectedDelay = $0 }
                }
                NotebookCallout(title: "A completion estimate is not clearance to start.",
                                message: "Every unresolved blocker remains visible until its handover is verified and the delay is resolved.",
                                symbol: "lock")
                NotebookRefreshFooter(lastChecked: viewModel.lastChecked, refresh: viewModel.refresh)
            }
            .padding(28).frame(maxWidth: 1250).frame(maxWidth: .infinity)
        }
        .background(NotebookStyle.bg).refreshable { viewModel.refresh() }
        .task(id: project.id) { viewModel.load(project: project) }
        .sheet(item: $selectedDelay) { record in
            DelaySummaryView(delay: record.summary, projectName: record.project.name,
                             detail: record.detail, completion: record.completion)
        }
    }
}
