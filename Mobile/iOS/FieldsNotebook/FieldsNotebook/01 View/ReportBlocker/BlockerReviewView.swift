import SwiftUI

struct BlockerReviewView: View {
    let viewModel: ReportBlockerViewModel
    let wide: Bool

    var body: some View {
        if wide {
            HStack(alignment: .top, spacing: 20) {
                BlockerReviewSummary(viewModel: viewModel).frame(maxWidth: .infinity)
                BlockerRecipientsFields(viewModel: viewModel).frame(maxWidth: .infinity)
            }
        } else {
            VStack(spacing: 20) {
                BlockerReviewSummary(viewModel: viewModel)
                BlockerRecipientsFields(viewModel: viewModel)
            }
        }
    }
}
