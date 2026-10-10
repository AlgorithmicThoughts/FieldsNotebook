import SwiftUI

struct BlockerRecipientsFields: View {
    @Bindable var viewModel: ReportBlockerViewModel

    var body: some View {
        NotebookSectionCard(title: "Notify people", symbol: "bell") {
            if let person = viewModel.responsibleCrew?.superintendent {
                NotebookInfoRow(title: person.name, detail: person.detail, symbol: "bell", value: "Required")
            }
            Text("The responsible Superintendent receives the report. Select anyone else who needs to see this blocker.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            ForEach(viewModel.optionalPeople) { person in
                Toggle(isOn: Binding(get: { viewModel.draft.additionalRecipientIDs.contains(person.id) }, set: { included in
                    if included { viewModel.draft.additionalRecipientIDs.insert(person.id) }
                    else { viewModel.draft.additionalRecipientIDs.remove(person.id) }
                })) {
                    VStack(alignment: .leading, spacing: 4) {
                        Text(person.name).font(.subheadline.weight(.semibold))
                        Text(person.detail).font(.caption).foregroundStyle(NotebookStyle.muted)
                    }
                }.frame(minHeight: 44)
            }
            Text("Selections apply to this report. This demo sends no notifications, email, or SMS.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }
}
