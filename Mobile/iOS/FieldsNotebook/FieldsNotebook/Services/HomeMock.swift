import Foundation

/// Local fixtures for the home screen. No workflow actions or network requests.
struct HomeMock: HomeProviding {
    let projects = [
        HomeProject(id: "OF-017", name: "Omaha Fabrication", place: "Omaha, Nebraska"),
        HomeProject(id: "WR-042", name: "West River Project", place: "Kansas City, Missouri"),
        HomeProject(id: "NC-063", name: "North Creek Expansion", place: "Lincoln, Nebraska")
    ]

    func loadHome(for project: HomeProject) -> HomeSnapshot {
        let user = HomeUser(name: "John Davis", initials: "JD", crew: "Crew B", role: "Foreman")
        switch project.id {
        case "OF-017":
            return HomeSnapshot(
                project: project, user: user, dateLabel: "Wednesday, 7 October",
                advanceNotice: HomeAdvanceNotice(
                    delayID: "FN-1042", headline: "Crew B cannot start the weld as planned.",
                    detail: "Crew A reports a delay to the Frame 12 fit-up. Your Superintendent coordinates the arrival plan.",
                    time: "2:00 PM", day: "Tomorrow · 8 October"
                ),
                delays: omahaDelays,
                activities: [
                    HomeActivity(id: "4821", title: "Access delay reopened", detail: "Crew B must acknowledge again.", time: "08:35 AM", delayID: "FN-1034"),
                    HomeActivity(id: "4818", title: "Frame 08 ready for verification", detail: "Crew B’s Foreman takes the next step.", time: "08:30 AM", delayID: "FN-1038"),
                    HomeActivity(id: "4815", title: "Tomorrow’s delay confirmed", detail: "Crew A estimates 2:00 PM.", time: "08:22 AM", delayID: "FN-1042")
                ], unreadNotificationCount: 2
            )
        case "WR-042":
            return HomeSnapshot(
                project: project, user: user, dateLabel: "Wednesday, 7 October",
                advanceNotice: HomeAdvanceNotice(
                    delayID: "FN-1041", headline: "Crew B cannot start the deck work as planned.",
                    detail: "Rain delays Crew A’s deck preparation. Your Superintendent coordinates the arrival plan.",
                    time: "11:00 AM", day: "Tomorrow · 8 October"
                ),
                delays: [HomeDelay(id: "FN-1041", title: "Rain delays the deck preparation", reason: "Adverse weather", reasonSymbol: "cloud.rain", responsibleCrew: "Crew A", affectedCrews: ["Crew B"], location: "South deck", status: .inProgress)],
                activities: [HomeActivity(id: "river-1", title: "Advance delay confirmed", detail: "Crew A estimates tomorrow at 11:00 AM.", time: "08:00 AM", delayID: "FN-1041")],
                unreadNotificationCount: 1
            )
        default:
            return HomeSnapshot(
                project: project, user: user, dateLabel: "Wednesday, 7 October", advanceNotice: nil,
                delays: [HomeDelay(id: "FN-1039", title: "Equipment fault delays the material lift", reason: "Equipment breakdown", reasonSymbol: "wrench.and.screwdriver", responsibleCrew: "Crew A", affectedCrews: ["Crew B"], location: "North work area", status: .blocked)],
                activities: [HomeActivity(id: "creek-1", title: "Material lift delay reported", detail: "Crew B is waiting on Crew A.", time: "07:30 AM", delayID: "FN-1039")],
                unreadNotificationCount: 1
            )
        }
    }

    private var omahaDelays: [HomeDelay] {
        [
            HomeDelay(id: "FN-1034", title: "Access to the fit-up area is blocked again", reason: "Safety issue", reasonSymbol: "exclamationmark.triangle.fill", responsibleCrew: "Crew B", affectedCrews: ["Crew A"], location: "East access lane", status: .blocked, isHighPriority: true),
            HomeDelay(id: "FN-1042", title: "Frame fit-up will delay tomorrow’s weld", reason: "Plan conflict", reasonSymbol: "doc.on.doc", responsibleCrew: "Crew A", affectedCrews: ["Crew B"], location: "Bay 4 · Frame 12", status: .inProgress),
            HomeDelay(id: "FN-1040", title: "Overhead crane is out of service", reason: "Equipment breakdown", reasonSymbol: "wrench.and.screwdriver", responsibleCrew: "Crew A", affectedCrews: ["Crew B", "Crew C"], location: "Bay 2 · Overhead crane", status: .blocked),
            HomeDelay(id: "FN-1038", title: "Frame 08 awaits handover verification", reason: "Pending inspection", reasonSymbol: "doc.text.magnifyingglass", responsibleCrew: "Crew A", affectedCrews: ["Crew B"], location: "Bay 3 · Frame 08", status: .verification)
        ]
    }
}
