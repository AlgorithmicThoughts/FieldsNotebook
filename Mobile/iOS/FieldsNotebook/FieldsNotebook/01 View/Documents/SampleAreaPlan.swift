import SwiftUI

struct SampleAreaPlan: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Label("ILLUSTRATIVE · NOT TO SCALE", systemImage: "map")
                .font(.caption.weight(.bold)).foregroundStyle(NotebookStyle.muted)
            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 14) {
                ForEach(1...4, id: \.self) { bay in
                    VStack(spacing: 8) {
                        Image(systemName: "building.2").font(.title2)
                        Text("Work area \(bay)").font(.subheadline.weight(.semibold))
                    }
                    .foregroundStyle(NotebookStyle.accent).frame(maxWidth: .infinity, minHeight: 90)
                    .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 12))
                }
            }
            Text("Sample access lane").font(.caption.weight(.semibold))
                .frame(maxWidth: .infinity, minHeight: 44)
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(NotebookStyle.control, style: StrokeStyle(lineWidth: 1, dash: [6])))
        }
        .padding(20).notebookCard()
    }
}
