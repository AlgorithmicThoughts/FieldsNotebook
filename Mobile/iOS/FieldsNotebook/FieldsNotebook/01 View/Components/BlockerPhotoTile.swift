import SwiftUI

struct BlockerPhotoTile: View {
    let photo: BlockerPhoto
    let number: Int
    let remove: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            if let image = UIImage(data: photo.thumbnailData) {
                Image(uiImage: image).resizable().scaledToFill().frame(height: 100).clipped()
                    .accessibilityLabel("Blocker evidence photo \(number)")
            }
            HStack {
                Text("Photo \(number)").font(.caption.weight(.semibold))
                Spacer()
                Button(action: remove) { Image(systemName: "trash").frame(minWidth: 44, minHeight: 44) }
                    .accessibilityLabel("Remove photo \(number)")
            }
            if let latitude = photo.latitude, let longitude = photo.longitude {
                Text("GPS \(latitude.formatted(.number.precision(.fractionLength(5)))), \(longitude.formatted(.number.precision(.fractionLength(5))))")
                    .font(.caption).foregroundStyle(NotebookStyle.muted)
            } else { Text("No photo GPS").font(.caption).foregroundStyle(NotebookStyle.muted) }
        }.padding(10).notebookCard()
    }
}
