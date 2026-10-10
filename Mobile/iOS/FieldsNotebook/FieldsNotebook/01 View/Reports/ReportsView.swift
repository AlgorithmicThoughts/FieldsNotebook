import SwiftUI
import FactoryKit
import UniformTypeIdentifiers

struct ReportsView: View {
    let project: HomeProject
    @InjectedObservable(\.reportsViewModel) private var viewModel
    @State private var selectedDelay: DelayRecord?
    @State private var exportDocument: ReportJSONDocument?
    @State private var showExporter = false
    @Environment(\.dynamicTypeSize) private var typeSize

    var body: some View {
        GeometryReader { geometry in
            ScrollView {
                VStack(alignment: .leading, spacing: 22) {
                    heading
                    if geometry.size.width > 850 && !typeSize.isAccessibilitySize {
                        HStack(alignment: .top, spacing: 20) {
                            ReportScopeCard(viewModel: viewModel).frame(width: 310)
                            results
                        }
                    } else {
                        ReportScopeCard(viewModel: viewModel)
                        results
                    }
                    NotebookCallout(title: "Sample report", message: "Exports contain local sample records. No live reporting API or external integration is connected.")
                }
                .padding(28).frame(maxWidth: 1250).frame(maxWidth: .infinity)
            }
            .background(NotebookStyle.bg)
        }
        .task(id: project.id) { viewModel.load(project: project) }
        .sheet(item: $selectedDelay) { record in
            DelaySummaryView(delay: record.summary, projectName: record.project.name,
                             detail: record.detail, completion: record.completion)
        }
        .fileExporter(isPresented: $showExporter, document: exportDocument, contentType: .json,
                      defaultFilename: "Field Notebook Sample Report") { result in
            switch result {
            case .success: viewModel.exportMessage = "Sample report exported."
            case .failure(let error): viewModel.exportMessage = "Could not save the report: \(error.localizedDescription)"
            }
        }
    }

    private var heading: some View {
        VStack(alignment: .leading, spacing: 16) {
            NotebookPageHeader(projectName: project.name, title: "Delay reports",
                               subtitle: "Review field records across your assigned projects.")
            Button(action: exportReport) { Label("Export sample JSON", systemImage: "arrow.up.right") }
                .buttonStyle(NotebookPrimaryButton()).disabled(viewModel.report == nil || viewModel.hasUnappliedChanges)
            if viewModel.hasUnappliedChanges {
                Text("Project selection changed. Apply filters to update the report.")
                    .font(.subheadline).foregroundStyle(NotebookStyle.muted)
            }
            if let message = viewModel.exportMessage { Text(message).font(.subheadline) }
        }
    }

    private var results: some View {
        VStack(alignment: .leading, spacing: 20) {
            if let report = viewModel.report {
                ReportSummaryCard(report: report)
                ReportReasonChart(report: report)
                reportRecords(report)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    private func reportRecords(_ report: NotebookReport) -> some View {
        VStack(spacing: 0) {
            Text("Included delay records").font(.headline)
                .frame(maxWidth: .infinity, alignment: .leading).padding(20)
            if report.records.isEmpty {
                ContentUnavailableView("No delay records", systemImage: "chart.bar",
                                       description: Text("No records were returned for this report scope."))
            }
            ForEach(report.records) { record in
                Divider().overlay(NotebookStyle.border)
                Button { selectedDelay = record } label: { DelayRecordRow(record: record) }
                    .buttonStyle(.plain)
            }
        }
        .notebookCard()
    }

    private func exportReport() {
        guard let data = viewModel.exportData() else { return }
        exportDocument = ReportJSONDocument(data: data)
        showExporter = true
    }
}
