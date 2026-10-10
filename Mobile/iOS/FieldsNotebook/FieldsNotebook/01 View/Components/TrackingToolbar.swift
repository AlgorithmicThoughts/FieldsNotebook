import SwiftUI

struct TrackingToolbar: View {
    @Bindable var viewModel: TrackingViewModel

    var body: some View {
        ViewThatFits(in: .horizontal) {
            HStack(spacing: 14) {
                scopePicker
                NotebookSearchField(placeholder: "Search delays, reasons, or IDs", query: $viewModel.query)
                refreshButton
            }
            VStack(alignment: .leading, spacing: 12) {
                HStack { scopePicker; Spacer(); refreshButton }
                NotebookSearchField(placeholder: "Search delays, reasons, or IDs", query: $viewModel.query)
            }
        }
    }

    private var scopePicker: some View {
        Picker("Tracking scope", selection: $viewModel.scope) {
            ForEach(TrackingScope.allCases) { Text($0.rawValue).tag($0) }
        }
        .pickerStyle(.menu).frame(minHeight: 44)
        .padding(.horizontal, 8).notebookCard()
    }

    private var refreshButton: some View {
        Button(action: viewModel.refresh) { Label("Refresh", systemImage: "arrow.clockwise").frame(minHeight: 44) }
            .buttonStyle(.bordered)
    }
}
