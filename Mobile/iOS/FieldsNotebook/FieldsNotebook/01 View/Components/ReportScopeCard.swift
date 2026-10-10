import SwiftUI

struct ReportScopeCard: View {
    @Bindable var viewModel: ReportsViewModel

    var body: some View {
        NotebookSectionCard(title: "Report scope", symbol: "building.2") {
            Text("Choose one or more assigned projects.").font(.subheadline).foregroundStyle(NotebookStyle.muted)
            ForEach(viewModel.projects) { project in
                Toggle(isOn: Binding(get: { viewModel.selectedProjectIDs.contains(project.id) },
                                     set: { viewModel.select(project, included: $0) })) {
                    VStack(alignment: .leading, spacing: 4) {
                        Text(project.name).font(.subheadline.weight(.semibold))
                        Text(project.id).font(.caption).foregroundStyle(NotebookStyle.muted)
                    }
                }
                .tint(NotebookStyle.accent).frame(minHeight: 44)
            }
            Divider()
            NotebookInfoRow(title: "Report dates", detail: "All dates in the selected projects.", symbol: "calendar", value: "All dates")
            if let error = viewModel.errorMessage {
                Label(error, systemImage: "exclamationmark.triangle.fill")
                    .font(.subheadline).foregroundStyle(NotebookStyle.urgent)
            }
            Button(action: viewModel.applyFilters) {
                Text("Apply filters").frame(maxWidth: .infinity)
            }
            .buttonStyle(NotebookPrimaryButton())
        }
    }
}
