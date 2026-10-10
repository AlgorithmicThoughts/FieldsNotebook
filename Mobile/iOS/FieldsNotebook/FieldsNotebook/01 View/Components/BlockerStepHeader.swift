import SwiftUI

struct BlockerStepHeader: View {
    let step: BlockerStep
    let project: HomeProject
    let user: HomeUser

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            ViewThatFits(in: .horizontal) {
                HStack { context; Spacer(); author }
                VStack(alignment: .leading, spacing: 8) { context; author }
            }
            HStack(alignment: .top, spacing: 12) {
                ForEach(BlockerStep.allCases) { item in
                    VStack(alignment: .leading, spacing: 6) {
                        Text("\(item.rawValue + 1). \(item.title)").font(.subheadline.weight(.semibold))
                            .foregroundStyle(item == step ? NotebookStyle.accent : NotebookStyle.muted)
                        Capsule().fill(item.rawValue <= step.rawValue ? NotebookStyle.accent : NotebookStyle.border).frame(height: 3)
                    }.frame(maxWidth: .infinity, alignment: .leading)
                        .accessibilityElement(children: .ignore)
                        .accessibilityLabel("Step \(item.rawValue + 1), \(item.title)")
                        .accessibilityAddTraits(item == step ? .isSelected : [])
                }
            }
        }
    }

    private var context: some View { Label(project.name, systemImage: "building.2").font(.subheadline.weight(.semibold)) }
    private var author: some View { Text("\(user.name) · \(user.crew)").font(.caption).foregroundStyle(NotebookStyle.muted) }
}
