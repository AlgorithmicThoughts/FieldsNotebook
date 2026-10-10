import SwiftUI

struct FeatureDestination: View {
    let tab: NotebookTab
    let snapshot: HomeSnapshot
    let trackingMetric: HomeMetric?
    let backToHome: () -> Void

    var body: some View {
        switch tab {
        case .tracking:
            TrackingView(project: snapshot.project, crew: snapshot.user.crew,
                         initialFilter: TrackingFilter(metric: trackingMetric))
        case .documents:
            DocumentsView(project: snapshot.project)
        case .upcoming:
            UpcomingView(project: snapshot.project)
        default:
            ContentUnavailableView {
                Label(tab.rawValue, systemImage: tab.symbol)
            } description: {
                Text("\(tab.rawValue) will be available in a future update.")
            } actions: {
                Button("Back to Home", action: backToHome).buttonStyle(NotebookPrimaryButton())
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity).background(NotebookStyle.bg)
        }
    }
}
