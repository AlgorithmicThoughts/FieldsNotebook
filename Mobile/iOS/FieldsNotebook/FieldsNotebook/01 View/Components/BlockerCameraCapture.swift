import SwiftUI
import UIKit

struct BlockerCameraCapture: UIViewControllerRepresentable {
    let complete: (Data?) -> Void

    func makeUIViewController(context: Context) -> UIImagePickerController {
        let picker = UIImagePickerController()
        picker.sourceType = .camera
        picker.cameraCaptureMode = .photo
        picker.delegate = context.coordinator
        return picker
    }

    func updateUIViewController(_ controller: UIImagePickerController, context: Context) {}
    func makeCoordinator() -> Coordinator { Coordinator(complete: complete) }

    final class Coordinator: NSObject, UIImagePickerControllerDelegate, UINavigationControllerDelegate {
        private let complete: (Data?) -> Void
        init(complete: @escaping (Data?) -> Void) { self.complete = complete }

        func imagePickerController(_ picker: UIImagePickerController, didFinishPickingMediaWithInfo info: [UIImagePickerController.InfoKey: Any]) {
            guard let image = info[.originalImage] as? UIImage, let data = image.jpegData(compressionQuality: 0.95) else {
                complete(nil)
                return
            }
            complete(data)
        }

        func imagePickerControllerDidCancel(_ picker: UIImagePickerController) { complete(nil) }
    }
}
