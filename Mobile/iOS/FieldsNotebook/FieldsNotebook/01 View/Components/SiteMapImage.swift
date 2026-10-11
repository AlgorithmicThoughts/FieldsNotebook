import SwiftUI

struct SiteMapImage: View {
    let image: UIImage

    var body: some View {
        Image(uiImage: image).resizable().scaledToFit()
            .accessibilityLabel("Sample site map")
    }
}
