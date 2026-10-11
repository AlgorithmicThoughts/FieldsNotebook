import SwiftUI
import FactoryKit
import AVFoundation

struct ReportBlockerSheet: View {
    @State private var viewModel: ReportBlockerViewModel
    @State private var confirmClose = false
    @Environment(\.scenePhase) private var scenePhase
    @Environment(\.dismiss) private var dismiss
    @Environment(\.dynamicTypeSize) private var typeSize
    let onSubmitted: () -> Void
    let openTracking: () -> Void

    init(project: HomeProject, user: HomeUser, onSubmitted: @escaping () -> Void, openTracking: @escaping () -> Void) {
        _viewModel = State(initialValue: Container.shared.reportBlockerViewModel((project, user)))
        self.onSubmitted = onSubmitted
        self.openTracking = openTracking
    }

    var body: some View {
        NavigationStack {
            GeometryReader { geometry in
                ScrollViewReader { proxy in
                    ScrollView {
                        VStack(alignment: .leading, spacing: 20) {
                            if let record = viewModel.submittedRecord {
                                BlockerSubmissionView(record: record, viewTracking: finishInTracking, done: { dismiss() })
                            } else { form(wide: geometry.size.width >= 740 && !typeSize.isAccessibilitySize) }
                        }.padding(24).id("formTop")
                    }
                    .scrollDismissesKeyboard(.interactively)
                    .onChange(of: viewModel.recording.isExpanded) { _, expanded in
                        if expanded { proxy.scrollTo("blockerRecording", anchor: .bottom) }
                    }
                    .onChange(of: viewModel.step) { _, _ in proxy.scrollTo("formTop", anchor: .top) }
                    .onChange(of: viewModel.issues) { _, issues in if !issues.isEmpty { proxy.scrollTo("formTop", anchor: .top) } }
                }
            }
            .background(NotebookStyle.bg)
            .navigationTitle(viewModel.submittedRecord == nil ? "Report a blocker" : "Report sent")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .cancellationAction) { Button("Close", action: close) } }
            .safeAreaInset(edge: .bottom, spacing: 0) {
                if viewModel.submittedRecord == nil {
                    BlockerSheetFooter(viewModel: viewModel, save: saveAndClose).disabled(viewModel.isLoadingPhotos || viewModel.recording.isActive)
                }
            }
        }
        .tint(NotebookStyle.accent).foregroundStyle(NotebookStyle.text).preferredColorScheme(.light)
        .presentationDetents([.large]).presentationSizing(.page).presentationDragIndicator(.visible)
        .interactiveDismissDisabled()
        .confirmationDialog("Keep this report draft?", isPresented: $confirmClose, titleVisibility: .visible) {
            Button("Save draft & close", action: saveAndClose)
            Button("Discard draft", role: .destructive) { viewModel.discardDraft(); dismiss() }
            Button("Keep editing", role: .cancel) {}
        }
        .onDisappear { viewModel.recording.cancel() }
        .onChange(of: scenePhase) { _, phase in if phase == .background { viewModel.recording.cancel() } }
        .onReceive(NotificationCenter.default.publisher(for: AVAudioSession.interruptionNotification)) { _ in
            if viewModel.recording.state == .recording { viewModel.recording.cancel() }
        }
        .onChange(of: viewModel.submittedRecord?.id) { _, id in if id != nil { onSubmitted() } }
    }

    private func form(wide: Bool) -> some View {
        VStack(alignment: .leading, spacing: 20) {
            BlockerStepHeader(step: viewModel.step, project: viewModel.project, user: viewModel.user)
            if !viewModel.issues.isEmpty {
                Text(viewModel.issues.joined(separator: "\n")).font(.subheadline).foregroundStyle(NotebookStyle.urgent)
                    .padding(16).frame(maxWidth: .infinity, alignment: .leading).background(NotebookStyle.urgentBG, in: RoundedRectangle(cornerRadius: 12))
            }
            if let message = viewModel.statusMessage { Text(message).font(.subheadline).foregroundStyle(NotebookStyle.muted) }
            stepContent(wide: wide)
        }
    }

    @ViewBuilder
    private func stepContent(wide: Bool) -> some View {
        switch viewModel.step {
        case .details: BlockerDetailsView(viewModel: viewModel, wide: wide)
        case .evidence: BlockerEvidenceView(viewModel: viewModel, wide: wide)
        case .review: BlockerReviewView(viewModel: viewModel, wide: wide)
        }
    }

    private func close() {
        viewModel.recording.cancel()
        if viewModel.submittedRecord != nil { dismiss() }
        else { confirmClose = true }
    }

    private func saveAndClose() { viewModel.saveDraft(); dismiss() }
    private func finishInTracking() { dismiss(); openTracking() }
}
