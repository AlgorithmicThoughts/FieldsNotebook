import Observation
import UIKit

@MainActor
@Observable
final class SiteMapViewModel {
    let image: UIImage?

    init(service: any SiteMediaProviding) { image = service.siteMapImage() }
}
