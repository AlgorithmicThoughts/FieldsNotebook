import SwiftUI
import PDFKit

struct NotebookPDFView: UIViewRepresentable {
    let document: PDFDocument

    func makeUIView(context: Context) -> PDFView {
        let view = PDFView()
        view.displayMode = .singlePageContinuous
        view.displayDirection = .vertical
        view.backgroundColor = UIColor(NotebookStyle.bg)
        view.document = document
        view.autoScales = true
        return view
    }

    func updateUIView(_ view: PDFView, context: Context) {
        guard view.document !== document else { return }
        view.document = document
        view.autoScales = true
    }
}
