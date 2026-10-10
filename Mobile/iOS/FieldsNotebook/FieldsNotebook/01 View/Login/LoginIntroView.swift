import SwiftUI

struct LoginIntroView: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 28) {
            NotebookBrand()
            VStack(alignment: .leading, spacing: 16) {
                Text("A clear handover.\nA confirmed start.")
                    .font(.largeTitle.weight(.bold)).fixedSize(horizontal: false, vertical: true)
                Text("Keep your crew in sync, follow every handoff, and see what’s ahead across your assigned projects.")
                    .font(.title3).foregroundStyle(NotebookStyle.muted)
            }
            VStack(alignment: .leading, spacing: 20) {
                Label("Track the delays affecting your crew", systemImage: "pause.circle")
                Label("Find project documents in one place", systemImage: "doc.text")
                Label("Plan tomorrow with today’s blockers", systemImage: "calendar")
            }
            .font(.subheadline.weight(.medium))
            .foregroundStyle(NotebookStyle.accent)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}
