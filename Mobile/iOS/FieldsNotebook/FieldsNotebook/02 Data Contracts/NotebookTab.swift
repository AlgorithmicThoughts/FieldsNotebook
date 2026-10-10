import Foundation

enum NotebookTab: String, CaseIterable, Identifiable {
    case home = "Home"
    case tracking = "Tracking"
    case siteMap = "Site map"
    case documents = "Documents"
    case upcoming = "Upcoming"
    case notifications = "Notifications"
    case reports = "Reports"
    case settings = "Settings"
    case more = "More"

    var id: Self { self }
    static var workspaceTabs: [Self] { allCases.filter { $0 != .more } }
    static let compactTabs: [Self] = [.home, .tracking, .siteMap, .documents, .more]
    static let moreTabs: [Self] = [.upcoming, .notifications, .reports, .settings]

    private static let symbols: [Self: String] = [
        .home: "house", .tracking: "list.bullet.rectangle", .siteMap: "map",
        .documents: "doc.text", .upcoming: "calendar", .notifications: "bell",
        .reports: "chart.bar", .settings: "gearshape", .more: "ellipsis"
    ]

    var symbol: String { Self.symbols[self] ?? "square" }
}

