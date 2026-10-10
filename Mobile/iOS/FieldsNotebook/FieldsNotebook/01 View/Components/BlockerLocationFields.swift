import SwiftUI

struct BlockerLocationFields: View {
    @Binding var draft: BlockerDraft

    var body: some View {
        NotebookSectionCard(title: "Site location", symbol: "mappin.and.ellipse") {
            BlockerSitePinPicker(pin: $draft.pin)
            Text("Location name").font(.subheadline.weight(.semibold))
            TextField("e.g. Bay 4, Frame 12", text: $draft.locationName)
                .textFieldStyle(.roundedBorder).frame(minHeight: 44)
                .accessibilityLabel("Location name")
                .onChange(of: draft.locationName) { _, value in draft.locationName = String(value.prefix(200)) }
            Text("Required when photos have no GPS.").font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }
}
