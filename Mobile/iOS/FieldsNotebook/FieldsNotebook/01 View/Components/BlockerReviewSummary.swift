import SwiftUI

struct BlockerReviewSummary: View {
    let viewModel: ReportBlockerViewModel

    var body: some View {
        NotebookSectionCard(title: "Review the blocker", symbol: "doc.text") {
            Text(viewModel.draft.description).font(.body).fixedSize(horizontal: false, vertical: true)
            Divider()
            NotebookInfoRow(title: "Responsible", detail: viewModel.responsibleCrew?.name ?? "", symbol: "person.2")
            NotebookInfoRow(title: "Affected crews", detail: viewModel.affectedCrews.map(\.name).joined(separator: ", "), symbol: "person.2")
            if !viewModel.selectedWorkItems.isEmpty {
                NotebookInfoRow(title: "Affected work", detail: viewModel.selectedWorkItems.map(\.title).joined(separator: ", "), symbol: "doc.on.doc")
            }
            NotebookInfoRow(title: "Reason", detail: viewModel.draft.reason, symbol: BlockerFixtures.reasonSymbol(viewModel.draft.reason))
            NotebookInfoRow(title: "Location", detail: viewModel.segmentName + " · " + viewModel.draft.locationName, symbol: "mappin.and.ellipse", value: "Manual pin")
            timing
            Divider()
            Text("\(viewModel.draft.photos.count) evidence photos").font(.subheadline.weight(.semibold))
            ScrollView(.horizontal) {
                HStack(spacing: 12) {
                    ForEach(Array(viewModel.draft.photos.enumerated()), id: \.element.id) { index, photo in
                        if let image = UIImage(data: photo.thumbnailData) {
                            Image(uiImage: image).resizable().scaledToFill().frame(width: 120, height: 90).clipped()
                                .clipShape(RoundedRectangle(cornerRadius: 10)).accessibilityLabel("Evidence photo \(index + 1)")
                        }
                    }
                }
            }
        }
    }

    @ViewBuilder
    private var timing: some View {
        if viewModel.draft.advanceNotice {
            NotebookInfoRow(title: "Advance delay · Tentative completion", detail: ReportBlockerMock.dateLabel(viewModel.draft.tentativeCompletion), symbol: "calendar")
            Text("The responsible Superintendent must confirm this estimate. It does not clear the blocker.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        } else { NotebookInfoRow(title: "Timing", detail: "Work is blocked now", symbol: "pause.circle") }
    }
}
