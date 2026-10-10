import SwiftUI

struct HomeView: View {
    let viewModel: HomeViewModel
    private var snapshot: HomeSnapshot { viewModel.snapshot }
    let openTracking: (HomeMetric?) -> Void
    let reportDelay: () -> Void
    @State private var selectedDelay: HomeDelay?
    @Environment(\.dynamicTypeSize) private var typeSize

    var body: some View {
        GeometryReader { geometry in
            ScrollView {
                VStack(alignment: .leading, spacing: 22) {
                    heading
                    if let notice = snapshot.advanceNotice {
                        AdvanceNoticeCard(notice: notice, wide: geometry.size.width > 650 && !typeSize.isAccessibilitySize) {
                            selectedDelay = snapshot.delays.first { $0.id == notice.delayID }
                        }
                    }
                    metrics(wide: geometry.size.width > 500 && !typeSize.isAccessibilitySize)
                    if geometry.size.width > 850 && !typeSize.isAccessibilitySize {
                        HStack(alignment: .top, spacing: 20) {
                            attention.frame(maxWidth: .infinity)
                            handoffs.frame(width: geometry.size.width * 0.32)
                        }
                    } else {
                        attention
                        handoffs
                    }
                    footer
                }
                .padding(geometry.size.width > 600 ? 28 : 16)
                .frame(maxWidth: 1250)
                .frame(maxWidth: .infinity)
            }
            .refreshable { viewModel.refresh() }
            .background(NotebookStyle.bg)
        }
        .sheet(item: $selectedDelay) { delay in
            DelaySummaryView(delay: delay, projectName: snapshot.project.name)
        }
    }

    private var heading: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text(snapshot.project.name.uppercased())
                .font(.caption.weight(.bold)).tracking(1.3)
                .foregroundStyle(NotebookStyle.muted)
            ViewThatFits(in: .horizontal) {
                HStack(alignment: .top, spacing: 16) {
                    title
                    Spacer(minLength: 0)
                    reportButton
                }
                VStack(alignment: .leading, spacing: 14) {
                    title
                    reportButton
                }
            }
        }
    }

    private var title: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Your crew, in sync.").font(.largeTitle.weight(.bold))
            Text("\(snapshot.user.crew) · \(snapshot.user.role) · \(snapshot.dateLabel)")
                .font(.subheadline).foregroundStyle(NotebookStyle.muted)
        }
        .fixedSize(horizontal: false, vertical: true)
    }

    private var reportButton: some View {
        Button(action: reportDelay) { Label("Report blocker", systemImage: "plus") }
            .buttonStyle(NotebookPrimaryButton())
    }

    private func metrics(wide: Bool) -> some View {
        let columns = Array(repeating: GridItem(.flexible(), spacing: 12), count: typeSize.isAccessibilitySize ? 1 : 3)
        return LazyVGrid(columns: columns, spacing: 12) {
            ForEach(HomeMetric.allCases) { metric in
                Button { openTracking(metric) } label: {
                    HomeMetricCard(metric: metric, count: snapshot.count(for: metric), wide: wide)
                }
                .buttonStyle(.plain)
            }
        }
    }

    private var attention: some View {
        VStack(spacing: 0) {
            HStack {
                Text("Needs attention").font(.headline)
                Spacer()
                Button { openTracking(nil) } label: {
                    Label("All delays", systemImage: "arrow.right")
                        .font(.subheadline.weight(.semibold)).frame(minHeight: 44)
                }
            }
            .padding(.horizontal, 20).padding(.vertical, 10)
            ForEach(snapshot.delays) { delay in
                Divider().overlay(NotebookStyle.border)
                Button { selectedDelay = delay } label: {
                    HomeDelayRow(delay: delay)
                }
                .buttonStyle(.plain)
            }
        }
        .notebookCard()
    }

    private var handoffs: some View {
        VStack(spacing: 16) {
            VStack(alignment: .leading, spacing: 20) {
                HStack {
                    Text("Latest handoffs").font(.headline)
                    Spacer()
                    Text("7 Oct").font(.caption.weight(.semibold))
                        .padding(.horizontal, 10).padding(.vertical, 5)
                        .background(NotebookStyle.bg, in: Capsule())
                }
                ForEach(snapshot.activities) { activity in
                    HStack(alignment: .top, spacing: 14) {
                        Circle().fill(NotebookStyle.accent).frame(width: 9, height: 9).padding(.top, 5)
                        VStack(alignment: .leading, spacing: 6) {
                            Text(activity.title).font(.subheadline.weight(.semibold))
                            Text(activity.detail).font(.subheadline).foregroundStyle(NotebookStyle.muted)
                            Text("\(activity.time) · \(activity.delayID)").font(.caption).foregroundStyle(NotebookStyle.muted)
                        }
                    }
                }
            }
            .padding(20).frame(maxWidth: .infinity, alignment: .leading).notebookCard()
            HStack(alignment: .top, spacing: 12) {
                Image(systemName: "checkmark.shield").font(.title3).foregroundStyle(NotebookStyle.accent)
                VStack(alignment: .leading, spacing: 6) {
                    Text("A clear handover. A confirmed start.").font(.subheadline.weight(.semibold))
                    Text("The affected crew’s Foreman verifies the handover. The responsible Superintendent then resolves the delay.")
                        .font(.subheadline).foregroundStyle(NotebookStyle.muted)
                }
            }
            .padding(18).frame(maxWidth: .infinity, alignment: .leading)
            .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 16))
        }
    }

    private var footer: some View {
        NotebookRefreshFooter(lastChecked: viewModel.lastChecked, refresh: viewModel.refresh)
    }

}

#Preview { ContentView() }
