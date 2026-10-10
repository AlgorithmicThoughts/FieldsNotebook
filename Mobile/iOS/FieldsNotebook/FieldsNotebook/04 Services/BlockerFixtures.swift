import Foundation

enum BlockerFixtures {
    static func options(for project: HomeProject) -> BlockerOptions {
        let prefix = project.id + "-"
        let names = project.id == "OF-017" ? ["Bay 4 · Fit-up & weld", "Bay 2 · Material transfer", "East access lane"] : ["Main work area", "Material transfer", "Access lane"]
        let segments = names.enumerated().map { BlockerSegment(id: prefix + "segment-\($0.offset)", name: $0.element) }
        let crewNames = project.id == "OF-017" ? ["Crew A", "Crew B", "Crew C"] : ["Crew A", "Crew B"]
        let supervisors = ["Alex Rivera", "Morgan Lee", "Taylor Brooks"]
        let crews = crewNames.enumerated().map { index, name in
            BlockerCrew(id: prefix + "crew-\(index)", name: name,
                        superintendent: BlockerPerson(id: prefix + "superintendent-\(index)", name: supervisors[index], detail: name + " · Superintendent"))
        }
        let items = crews.enumerated().map { index, crew in
            BlockerWorkItem(id: prefix + "work-\(index)", title: workTitle(index, project: project),
                            crewID: crew.id, segmentID: segments[0].id)
        }
        let people = crews.map(\.superintendent) + [BlockerPerson(id: prefix + "foreman-a", name: "Sam Carter", detail: "Crew A · Foreman")]
        return BlockerOptions(crews: crews, segments: segments, workItems: items, people: people)
    }

    private static func workTitle(_ index: Int, project: HomeProject) -> String {
        if project.id == "OF-017" { return ["Fit up Frame 12", "Weld Frame 12", "Move the fitted frame"][index] }
        return index == 0 ? "Prepare the work area" : "Complete the next crew activity"
    }

    static func reasonSymbol(_ reason: String) -> String { symbols[reason] ?? "doc.text" }
    private static let symbols = [
        "Missing material": "shippingbox", "Safety issue": "exclamationmark.triangle.fill",
        "Personnel shortage": "person.2", "Quality concern": "doc.text.magnifyingglass",
        "Plan conflict": "doc.on.doc", "Pending inspection": "doc.text.magnifyingglass",
        "Equipment breakdown": "wrench.and.screwdriver", "Adverse weather": "cloud.rain", "Other": "doc.text"
    ]
}
