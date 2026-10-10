import SwiftUI

struct BlockerSheetFooter: View {
    let viewModel: ReportBlockerViewModel
    let save: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Sample report · Drafts and submissions stay in this demo session.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            ViewThatFits(in: .horizontal) {
                HStack(spacing: 12) { actions }
                VStack(alignment: .leading, spacing: 12) { actions }
            }
        }
        .padding(.horizontal, 24).padding(.vertical, 14).frame(maxWidth: .infinity)
        .background(NotebookStyle.surface)
        .overlay(alignment: .top) { Divider().overlay(NotebookStyle.border) }
    }

    @ViewBuilder
    private var actions: some View {
        Button(action: save) { Label("Save draft", systemImage: "doc.text").frame(minHeight: 44) }
        if viewModel.step != .details { Button("Back", action: viewModel.back).frame(minHeight: 44) }
        if viewModel.step == .review {
            Button(action: viewModel.submit) { Label("Send report", systemImage: "paperplane") }.buttonStyle(NotebookPrimaryButton())
        } else {
            Button(action: viewModel.next) { Label(viewModel.step == .details ? "Next: Evidence" : "Review report", systemImage: "arrow.right") }
                .buttonStyle(NotebookPrimaryButton())
        }
    }
}
