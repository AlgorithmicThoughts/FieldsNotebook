import SwiftUI

struct BlockerDetailsView: View {
    @Bindable var viewModel: ReportBlockerViewModel
    let wide: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            BlockerDescriptionField(viewModel: viewModel)
            if wide {
                HStack(alignment: .top, spacing: 20) {
                    BlockerCrewFields(viewModel: viewModel).frame(maxWidth: .infinity)
                    VStack(spacing: 20) { reasonsAndTiming }.frame(maxWidth: .infinity)
                }
            } else {
                BlockerCrewFields(viewModel: viewModel)
                reasonsAndTiming
            }
        }
    }

    @ViewBuilder
    private var reasonsAndTiming: some View {
        BlockerReasonPicker(reason: $viewModel.draft.reason)
        BlockerTimingFields(draft: $viewModel.draft)
    }
}
