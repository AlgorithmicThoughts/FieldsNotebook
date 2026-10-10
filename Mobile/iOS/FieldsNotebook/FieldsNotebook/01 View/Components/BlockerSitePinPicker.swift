import SwiftUI

struct BlockerSitePinPicker: View {
    @Binding var pin: BlockerPin?

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Tap the sample plan to place a pin.").font(.caption).foregroundStyle(NotebookStyle.muted)
            GeometryReader { geometry in
                ZStack {
                    plan
                    if let pin {
                        Image(systemName: "mappin.and.ellipse").font(.title).foregroundStyle(NotebookStyle.urgent)
                            .position(x: pin.x * geometry.size.width, y: pin.y * geometry.size.height)
                    }
                }
                .contentShape(Rectangle())
                .gesture(SpatialTapGesture().onEnded { event in
                    pin = BlockerPin(x: min(1, max(0, event.location.x / geometry.size.width)),
                                     y: min(1, max(0, event.location.y / geometry.size.height)))
                })
            }.aspectRatio(1.6, contentMode: .fit)
                .accessibilityElement(children: .ignore)
                .accessibilityLabel("Sample site map")
                .accessibilityValue(pin == nil ? "No pin selected" : "Manual pin selected")
                .accessibilityAction(named: "Place pin at center") { pin = BlockerPin(x: 0.5, y: 0.5) }
            Button { pin = BlockerPin(x: 0.5, y: 0.5) } label: { Label("Place pin at center", systemImage: "mappin.and.ellipse").frame(minHeight: 44) }
            Text(pin == nil ? "No pin selected" : "Manual pin selected · Sample map")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            Text("Illustrative map · Not to scale. Photo GPS is kept separately and does not place a pin on this plan.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }

    private var plan: some View {
        VStack(spacing: 10) {
            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 10) {
                ForEach(1...4, id: \.self) { bay in
                    Text("Bay \(bay)").font(.subheadline.weight(.semibold)).frame(maxWidth: .infinity, minHeight: 66)
                        .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 8))
                }
            }
            Text("Access lane").font(.caption).frame(maxWidth: .infinity, minHeight: 36)
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(NotebookStyle.control, style: StrokeStyle(lineWidth: 1, dash: [5])))
        }.padding(12).frame(maxWidth: .infinity, maxHeight: .infinity)
            .background(NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 12))
    }
}
