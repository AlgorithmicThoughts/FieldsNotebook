import Observation
import PDFKit
import UIKit

@MainActor
@Observable
final class DocumentPreviewViewModel {
    let document: SiteDocument
    let pdf: PDFDocument?
    let siteImage: UIImage?

    init(document: SiteDocument, service: any SiteMediaProviding) {
        self.document = document
        pdf = document.pdfResourceName.flatMap { service.pdf(named: $0) }
        siteImage = document.category == .plans ? service.siteMapImage() : nil
    }
}
