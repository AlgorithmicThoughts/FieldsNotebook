import SwiftUI

struct ProjectSelector: View {
    let selected: HomeProject
    let projects: [HomeProject]
    let select: (HomeProject) -> Void

    var body: some View {
        Menu {
            ForEach(projects) { project in
                Button { select(project) } label: {
                    if selected == project {
                        Label(project.name, systemImage: "checkmark")
                    } else {
                        Text(project.name)
                    }
                }
            }
        } label: {
            HStack(spacing: 10) {
                Image(systemName: "building.2").foregroundStyle(NotebookStyle.accent)
                VStack(alignment: .leading, spacing: 3) {
                    Text(selected.name).font(.subheadline.weight(.semibold))
                    Text("\(selected.id) · \(selected.place)").font(.caption2).foregroundStyle(NotebookStyle.muted)
                }
                Image(systemName: "chevron.up.chevron.down").font(.caption)
            }
            .padding(10).frame(minHeight: 44)
            .background(NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 12))
            .overlay(RoundedRectangle(cornerRadius: 12).stroke(NotebookStyle.border, lineWidth: 1))
        }
        .accessibilityLabel("Change project, \(selected.name)")
    }
}
