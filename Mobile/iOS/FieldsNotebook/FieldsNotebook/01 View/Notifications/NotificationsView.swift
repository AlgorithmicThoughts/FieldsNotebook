import SwiftUI
import FactoryKit

struct NotificationsView: View {
    let project: HomeProject
    @InjectedObservable(\.notificationsViewModel) private var viewModel
    @State private var selectedDelay: DelayRecord?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                NotebookPageHeader(projectName: project.name, title: "Notifications",
                                   subtitle: "Your crew’s handovers and delay updates, in one place.")
                HStack {
                    NotebookFilterBar(options: NoticeFilter.allCases, selection: $viewModel.filter)
                    Text("\(viewModel.unreadCount) unread").font(.subheadline.weight(.semibold))
                        .foregroundStyle(NotebookStyle.accent).fixedSize()
                }
                noticeList
                if let error = viewModel.errorMessage {
                    NotebookCallout(title: "Unable to open notice", message: error, symbol: "exclamationmark.triangle.fill")
                }
                NotebookCallout(title: "Opening a notice marks only that notice read.",
                                message: "It does not acknowledge, verify, or resolve the delay. These are in-app sample notices.")
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

    private var noticeList: some View {
        VStack(spacing: 0) {
            if viewModel.visibleNotices.isEmpty {
                ContentUnavailableView("You’re up to date", systemImage: "bell",
                                       description: Text("No notices match this view in the selected project."))
            } else {
                ForEach(viewModel.visibleNotices) { notice in
                    NotificationRow(notice: notice) { selectedDelay = viewModel.open(notice) }
                    Divider().overlay(NotebookStyle.border)
                }
            }
        }
        .notebookCard()
    }
}
