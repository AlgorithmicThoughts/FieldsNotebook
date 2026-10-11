import PDFKit
import UIKit

@MainActor
protocol SiteMediaProviding {
    func siteMapImage() -> UIImage?
    func pdf(named resource: String) -> PDFDocument?
}

/// The same bundled sample media used by the web UI. No project API contract is implied.
@MainActor
final class SiteMediaService: SiteMediaProviding {
    func siteMapImage() -> UIImage? { UIImage(named: "Site-map") }

    func pdf(named resource: String) -> PDFDocument? {
        guard let url = Bundle.main.url(forResource: resource, withExtension: "pdf") else { return nil }
        return PDFDocument(url: url)
    }
}
