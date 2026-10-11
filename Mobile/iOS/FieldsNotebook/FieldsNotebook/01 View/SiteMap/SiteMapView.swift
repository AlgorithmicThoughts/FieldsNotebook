import SwiftUI
import FactoryKit

struct SiteMapView: View {
    let project: HomeProject
    @InjectedObservable(\.siteMapViewModel) private var viewModel

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                NotebookPageHeader(projectName: project.name, title: "Site map", subtitle: "View the site layout for your project.")
                NotebookSectionCard(title: project.name + " · Site layout", symbol: "map") {
                    if let image = viewModel.image {
                        SiteMapImage(image: image).clipShape(RoundedRectangle(cornerRadius: 12))
                    } else {
                        ContentUnavailableView("Site image unavailable", systemImage: "map",
                                               description: Text("The sample site image could not be loaded."))
                    }
                    Text("Sample site image · The same illustrative layout is used for each demo project.")
                        .font(.caption).foregroundStyle(NotebookStyle.muted)
                }
            }.padding(28).frame(maxWidth: 1250).frame(maxWidth: .infinity)
        }.background(NotebookStyle.bg)
    }
}
