import SwiftUI

struct BlockerCrewFields: View {
    @Bindable var viewModel: ReportBlockerViewModel

    var body: some View {
        NotebookSectionCard(title: "Crews & work", symbol: "person.2") {
            Text("Responsible crew").font(.subheadline.weight(.semibold))
            Picker("Responsible crew", selection: $viewModel.draft.responsibleCrewID) {
                ForEach(viewModel.options.crews) { Text($0.name).tag($0.id) }
            }.frame(minHeight: 44)
            Text("This crew must remove the blocker.").font(.caption).foregroundStyle(NotebookStyle.muted)
            Divider()
            Text("Segment").font(.subheadline.weight(.semibold))
            Picker("Segment", selection: $viewModel.draft.segmentID) {
                Text("Choose a segment").tag("")
                ForEach(viewModel.options.segments) { Text($0.name).tag($0.id) }
            }.frame(minHeight: 44)
                .onChange(of: viewModel.draft.segmentID) { _, _ in viewModel.reconcileWorkItems() }
            Text("Affected crews").font(.subheadline.weight(.semibold))
            Text("Select every crew that cannot start or continue.").font(.caption).foregroundStyle(NotebookStyle.muted)
            ForEach(viewModel.options.crews) { crew in
                Toggle(crew.name, isOn: Binding(get: { viewModel.draft.affectedCrewIDs.contains(crew.id) },
                                               set: { viewModel.setAffected(crew, included: $0) }))
                    .frame(minHeight: 44)
                if viewModel.draft.affectedCrewIDs.contains(crew.id) { workItems(crew) }
            }
        }
    }

    private func workItems(_ crew: BlockerCrew) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Affected work items (optional)").font(.caption).foregroundStyle(NotebookStyle.muted)
            if viewModel.workItems(for: crew).isEmpty {
                Text("No sample work items in this segment.").font(.caption).foregroundStyle(NotebookStyle.muted)
            }
            ForEach(viewModel.workItems(for: crew)) { item in
                Toggle(item.title, isOn: Binding(get: { viewModel.draft.workItemIDs.contains(item.id) }, set: { included in
                    if included { viewModel.draft.workItemIDs.insert(item.id) }
                    else { viewModel.draft.workItemIDs.remove(item.id) }
                })).frame(minHeight: 44)
            }
        }.padding(.leading, 16)
    }
}
