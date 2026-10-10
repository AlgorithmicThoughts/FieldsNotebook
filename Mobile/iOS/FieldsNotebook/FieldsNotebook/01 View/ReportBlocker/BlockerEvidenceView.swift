import SwiftUI

struct BlockerEvidenceView: View {
    @Bindable var viewModel: ReportBlockerViewModel
    let wide: Bool

    var body: some View {
        if wide {
            HStack(alignment: .top, spacing: 20) {
                BlockerPhotoSection(viewModel: viewModel).frame(maxWidth: .infinity)
                BlockerLocationFields(draft: $viewModel.draft).frame(maxWidth: .infinity)
            }
        } else {
            VStack(spacing: 20) {
                BlockerPhotoSection(viewModel: viewModel)
                BlockerLocationFields(draft: $viewModel.draft)
            }
        }
    }
}
