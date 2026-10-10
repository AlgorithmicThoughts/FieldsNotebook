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
            HomeDelaySummary(delay: delay, projectName: snapshot.project.name)
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
        Button(action: reportDelay) { Label("Report delay", systemImage: "plus") }
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
        VStack(alignment: .leading, spacing: 8) {
            Button(action: viewModel.refresh) {
                Label("Last check \(viewModel.lastChecked.formatted(Self.checkTime)) · Refresh", systemImage: "arrow.clockwise")
                    .font(.caption).frame(minHeight: 44)
            }
            Text("Sample data · 7 Oct 2026 · Project time: Central")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }

    private static var checkTime: Date.FormatStyle {
        Date.FormatStyle(date: .omitted, time: .shortened, timeZone: TimeZone(identifier: "America/Chicago")!)
    }
}

private struct AdvanceNoticeCard: View {
    let notice: HomeAdvanceNotice
    let wide: Bool
    let openDelay: () -> Void

    var body: some View {
        Group {
            if wide {
                HStack(spacing: 0) {
                    content.frame(maxWidth: .infinity, alignment: .leading)
                    estimate.frame(width: 220)
                }
            } else {
                VStack(alignment: .leading, spacing: 0) {
                    content
                    estimate.frame(maxWidth: .infinity, alignment: .leading)
                }
            }
        }
        .notebookCard()
    }

    private var content: some View {
        VStack(alignment: .leading, spacing: 15) {
            Label("AHEAD OF TOMORROW", systemImage: "calendar")
                .font(.caption.weight(.bold)).tracking(0.7)
                .padding(.horizontal, 10).padding(.vertical, 7)
                .foregroundStyle(NotebookStyle.blocked)
                .background(NotebookStyle.blockedBG, in: Capsule())
            Text(notice.headline).font(.title2.weight(.bold))
                .fixedSize(horizontal: false, vertical: true)
            Text(notice.detail).font(.subheadline).foregroundStyle(NotebookStyle.muted)
            Button(action: openDelay) { Label("View delay", systemImage: "arrow.right") }
                .buttonStyle(NotebookPrimaryButton())
            Text(notice.delayID + " · Crew A → Crew B").font(.caption).foregroundStyle(NotebookStyle.muted)
        }
        .padding(24).frame(maxWidth: .infinity, alignment: .leading)
        .overlay(alignment: .leading) { Rectangle().fill(NotebookStyle.accent).frame(width: 4) }
    }

    private var estimate: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("TENTATIVE COMPLETION").font(.caption.weight(.bold)).tracking(1)
                .foregroundStyle(NotebookStyle.muted)
            Text(notice.time).font(.largeTitle.weight(.bold))
            Text(notice.day).font(.subheadline.weight(.semibold))
            Label("An estimate is not clearance to start.", systemImage: "lock")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(24).frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .leading)
        .background(NotebookStyle.soft)
    }
}

private struct HomeMetricCard: View {
    let metric: HomeMetric
    let count: Int
    let wide: Bool

    var body: some View {
        HStack(spacing: 14) {
            if wide {
                Image(systemName: metric.symbol).font(.title3)
                    .foregroundStyle(NotebookStyle.accent)
                    .frame(width: 42, height: 42)
                    .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 11))
            }
            VStack(alignment: .leading, spacing: 5) {
                Text(count, format: .number).font(.largeTitle.weight(.bold))
                Text(metric.rawValue).font(.caption).foregroundStyle(NotebookStyle.muted)
                    .frame(minHeight: 30, alignment: .top)
            }
            Spacer(minLength: 0)
        }
        .padding(wide ? 20 : 12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .notebookCard()
        .accessibilityElement(children: .combine)
    }
}

struct HomeDelayRow: View {
    let delay: HomeDelay
    @Environment(\.horizontalSizeClass) private var sizeClass

    var body: some View {
        let colors = delay.isHighPriority ? (NotebookStyle.urgent, NotebookStyle.urgentBG) : (NotebookStyle.accent, NotebookStyle.soft)
        HStack(alignment: .top, spacing: 14) {
            if sizeClass == .regular {
                Image(systemName: delay.isHighPriority ? "pin.fill" : delay.reasonSymbol)
                    .font(.title3).foregroundStyle(colors.0)
                    .frame(width: 40, height: 40)
                    .background(colors.1, in: RoundedRectangle(cornerRadius: 11))
            }
            VStack(alignment: .leading, spacing: 8) {
                Text("\(delay.id) · \(delay.reason)").font(.caption).foregroundStyle(NotebookStyle.muted)
                if delay.isHighPriority {
                    Label("High priority · Reopened", systemImage: "pin.fill")
                        .font(.caption.weight(.semibold)).foregroundStyle(NotebookStyle.urgent)
                }
                Text(delay.title).font(.subheadline.weight(.semibold))
                    .fixedSize(horizontal: false, vertical: true)
                Text("\(delay.crewSummary) · \(delay.location)")
                    .font(.caption).foregroundStyle(NotebookStyle.muted)
                HomeStatusBadge(status: delay.status)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            Image(systemName: "chevron.right").font(.caption.weight(.semibold))
                .foregroundStyle(NotebookStyle.muted).padding(.top, 4)
        }
        .padding(20).contentShape(Rectangle())
        .accessibilityElement(children: .combine)
    }
}

struct HomeStatusBadge: View {
    let status: HomeDelayStatus

    private var colors: (Color, Color) {
        switch status {
        case .blocked: (NotebookStyle.blocked, NotebookStyle.blockedBG)
        case .inProgress: (NotebookStyle.progress, NotebookStyle.progressBG)
        case .verification: (NotebookStyle.verify, NotebookStyle.verifyBG)
        }
    }

    var body: some View {
        Label(status.rawValue, systemImage: status.symbol)
            .font(.caption.weight(.semibold))
            .padding(.horizontal, 9).padding(.vertical, 6)
            .foregroundStyle(colors.0).background(colors.1, in: Capsule())
    }
}

private struct HomeDelaySummary: View {
    let delay: HomeDelay
    let projectName: String
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 18) {
                    Text(projectName).font(.subheadline).foregroundStyle(NotebookStyle.muted)
                    Text(delay.title).font(.title2.weight(.bold))
                    HomeStatusBadge(status: delay.status)
                    if delay.isHighPriority {
                        Label("High priority · Reopened", systemImage: "pin.fill").foregroundStyle(NotebookStyle.urgent)
                    }
                    Label(delay.reason, systemImage: delay.reasonSymbol)
                    Label(delay.crewSummary, systemImage: "person.2")
                    Label(delay.location, systemImage: "mappin.and.ellipse")
                    Text("The affected crew’s Foreman must verify the handover before the responsible Superintendent resolves the delay.")
                        .font(.subheadline).foregroundStyle(NotebookStyle.muted)
                }
                .padding(24).frame(maxWidth: .infinity, alignment: .leading)
            }
            .background(NotebookStyle.bg)
            .navigationTitle(delay.id).navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .confirmationAction) { Button("Done") { dismiss() } } }
        }
        .presentationDetents([.medium, .large])
        .preferredColorScheme(.light)
    }
}

#Preview { ContentView() }
