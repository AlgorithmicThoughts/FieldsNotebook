import Foundation
import ImageIO
import UniformTypeIdentifiers

actor BlockerPhotoLoader {
    func photo(from data: Data) throws -> BlockerPhoto {
        try Task.checkCancellation()
        guard let source = CGImageSourceCreateWithData(data as CFData, nil),
              let thumbnail = CGImageSourceCreateThumbnailAtIndex(source, 0, [
                kCGImageSourceCreateThumbnailFromImageAlways: true,
                kCGImageSourceCreateThumbnailWithTransform: true,
                kCGImageSourceThumbnailMaxPixelSize: 400
              ] as CFDictionary) else { throw BlockerFormError.invalid("This photo could not be opened. Choose another image.") }
        let output = NSMutableData()
        guard let destination = CGImageDestinationCreateWithData(output, UTType.jpeg.identifier as CFString, 1, nil) else {
            throw BlockerFormError.invalid("Could not prepare the photo preview.")
        }
        CGImageDestinationAddImage(destination, thumbnail, nil)
        guard CGImageDestinationFinalize(destination) else { throw BlockerFormError.invalid("Could not prepare the photo preview.") }
        try Task.checkCancellation()
        let gps = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any]
        let location = gps?[kCGImagePropertyGPSDictionary] as? [CFString: Any]
        return BlockerPhoto(id: UUID(), originalData: data, thumbnailData: output as Data,
                            latitude: coordinate(location, value: kCGImagePropertyGPSLatitude, reference: kCGImagePropertyGPSLatitudeRef),
                            longitude: coordinate(location, value: kCGImagePropertyGPSLongitude, reference: kCGImagePropertyGPSLongitudeRef))
    }

    private func coordinate(_ gps: [CFString: Any]?, value: CFString, reference: CFString) -> Double? {
        guard let number = gps?[value] as? Double, number.isFinite else { return nil }
        let direction = gps?[reference] as? String
        return ["S", "W"].contains(direction ?? "") ? -abs(number) : number
    }
}
