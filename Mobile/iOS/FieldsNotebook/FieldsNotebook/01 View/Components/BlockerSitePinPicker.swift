import SwiftUI
import FactoryKit

struct BlockerSitePinPicker: View {
    @Binding var pin: BlockerPin?
    @InjectedObservable(\.siteMapViewModel) private var viewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Tap the sample site image to place a pin.").font(.caption).foregroundStyle(NotebookStyle.muted)
            if let image = viewModel.image { pinSurface(image) }
            else { ContentUnavailableView("Site image unavailable", systemImage: "map") }
            Button { pin = BlockerPin(x: 0.5, y: 0.5) } label: {
                Label("Place pin at center", systemImage: "mappin.and.ellipse").frame(minHeight: 44)
            }.disabled(viewModel.image == nil)
            Text(pin == nil ? "No pin selected" : "Manual pin selected · Sample map")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            Text("Illustrative image · Photo GPS is kept separately and does not place a pin on this image.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }

    private func pinSurface(_ image: UIImage) -> some View {
        GeometryReader { geometry in
            ZStack {
                SiteMapImage(image: image)
                if let pin {
                    Image(systemName: "mappin.and.ellipse").font(.title).foregroundStyle(NotebookStyle.urgent)
                        .position(x: pin.x * geometry.size.width, y: pin.y * geometry.size.height)
                }
            }
            .frame(width: geometry.size.width, height: geometry.size.height)
            .contentShape(Rectangle())
            .gesture(SpatialTapGesture().onEnded { event in
                guard geometry.size.width > 0, geometry.size.height > 0 else { return }
                pin = BlockerPin(x: min(1, max(0, event.location.x / geometry.size.width)),
                                 y: min(1, max(0, event.location.y / geometry.size.height)))
            })
        }.aspectRatio(image.size.width / image.size.height, contentMode: .fit)
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .accessibilityElement(children: .ignore)
            .accessibilityLabel("Sample site map")
            .accessibilityValue(pin == nil ? "No pin selected" : "Manual pin selected")
            .accessibilityAction(named: "Place pin at center") { pin = BlockerPin(x: 0.5, y: 0.5) }
    }
}
