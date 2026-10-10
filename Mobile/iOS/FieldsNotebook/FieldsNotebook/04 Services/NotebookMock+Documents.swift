import Foundation

extension NotebookMock {
    func documents(for project: HomeProject) -> [SiteDocument] {
        [safetyDocument(project), scheduleDocument(project), planDocument(project)]
    }

    private func safetyDocument(_ project: HomeProject) -> SiteDocument {
        SiteDocument(
            id: project.id + "-safety", project: project, title: "Site safety plan", category: .safety,
            symbol: "checkmark.shield", revision: "Sample · Rev 03", note: "Project safety reference",
            sections: [
                DocumentSection(id: "contacts", title: "Site contacts", detail: "Sample section. Use the approved project directory for actual site contacts."),
                DocumentSection(id: "hazards", title: "Hazard controls", detail: "Sample section. This preview contains no approved hazard-control instructions."),
                DocumentSection(id: "response", title: "Emergency response", detail: "Sample section. Refer to the approved site emergency plan."),
                DocumentSection(id: "permits", title: "Access and permits", detail: "Sample section. Confirm current access requirements with the project team.")
            ]
        )
    }

    private func scheduleDocument(_ project: HomeProject) -> SiteDocument {
        let sections = upcoming(for: project).map { work in
            DocumentSection(id: work.id, title: "\(work.plannedTime) · \(work.crew) · \(work.title)",
                            detail: "Delay reported. Tentative completion: \(work.completion). Confirm every blocker before starting.")
        }
        return SiteDocument(id: project.id + "-schedule", project: project, title: "Work schedule", category: .schedule,
                            symbol: "calendar", revision: "Sample · 8 October", note: "Planned crew handovers", sections: sections)
    }

    private func planDocument(_ project: HomeProject) -> SiteDocument {
        SiteDocument(
            id: project.id + "-plan", project: project,
            title: project.id == "OF-017" ? "Fabrication area plan" : "Work area plan", category: .plans,
            symbol: "doc.text", revision: "Sample · Rev 02", note: "Work areas and access layout",
            sections: [DocumentSection(id: "layout", title: "Illustrative layout", detail: "Sample work areas for \(project.name). This drawing has no surveyed coordinates, dimensions, or approved construction information.")]
        )
    }
}
