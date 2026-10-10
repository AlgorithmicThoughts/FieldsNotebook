import SwiftUI

struct HomeStatusBadge: View {
    let status: HomeDelayStatus

    private var colors: (Color, Color) {
        switch status {
        case .blocked: (NotebookStyle.blocked, NotebookStyle.blockedBG)
        case .inProgress: (NotebookStyle.progress, NotebookStyle.progressBG)
        case .verification: (NotebookStyle.verify, NotebookStyle.verifyBG)
        case .resolved: (NotebookStyle.resolved, NotebookStyle.resolvedBG)
        }
    }

    var body: some View {
        Label(status.rawValue, systemImage: status.symbol)
            .font(.caption.weight(.semibold))
            .padding(.horizontal, 9).padding(.vertical, 6)
            .foregroundStyle(colors.0).background(colors.1, in: Capsule())
    }
}

