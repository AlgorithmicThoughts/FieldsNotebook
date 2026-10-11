import FactoryKit

extension Container {
    @MainActor
    var siteMediaService: Factory<any SiteMediaProviding> {
        self { @MainActor in SiteMediaService() }.shared
    }

    @MainActor
    var siteMapViewModel: Factory<SiteMapViewModel> {
        self { @MainActor in SiteMapViewModel(service: self.siteMediaService()) }
    }

    @MainActor
    var documentPreviewViewModel: ParameterFactory<SiteDocument, DocumentPreviewViewModel> {
        self { @MainActor document in DocumentPreviewViewModel(document: document, service: self.siteMediaService()) }
    }
}
