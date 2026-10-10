import SwiftUI
import PhotosUI
import AVFoundation

struct BlockerPhotoSection: View {
    @Bindable var viewModel: ReportBlockerViewModel
    @State private var selection: [PhotosPickerItem] = []
    @State private var showCamera = false
    @State private var cameraData: Data?
    @State private var cameraMessage: String?

    var body: some View {
        NotebookSectionCard(title: "Photos", symbol: "photo.on.rectangle") {
            HStack {
                Text("At least 1 photo · Maximum 5").font(.caption).foregroundStyle(NotebookStyle.muted)
                Spacer()
                Text("\(viewModel.draft.photos.count)/5").font(.subheadline.weight(.semibold))
            }
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 150))], spacing: 12) {
                ForEach(Array(viewModel.draft.photos.enumerated()), id: \.element.id) { index, photo in
                    BlockerPhotoTile(photo: photo, number: index + 1) { viewModel.draft.photos.removeAll { $0.id == photo.id } }
                }
            }
            photoActions
            if viewModel.isLoadingPhotos { ProgressView("Preparing photos…") }
            if let message = cameraMessage { Text(message).font(.caption).foregroundStyle(NotebookStyle.muted) }
        }
        .task(id: selection) { await importSelection() }
        .task(id: cameraData) {
            guard let cameraData else { return }
            await viewModel.addPhotos([cameraData])
        }
        .sheet(isPresented: $showCamera) {
            BlockerCameraCapture { data in
                cameraData = data
                showCamera = false
            }.ignoresSafeArea()
        }
    }

    private var photoActions: some View {
        ViewThatFits(in: .horizontal) {
            HStack(spacing: 12) { actionButtons }
            VStack(alignment: .leading, spacing: 12) { actionButtons }
        }.disabled(viewModel.isLoadingPhotos || viewModel.draft.photos.count >= 5)
    }

    @ViewBuilder
    private var actionButtons: some View {
        Button(action: takePhoto) { Label("Take photo", systemImage: "camera").frame(minHeight: 44) }
            .buttonStyle(.bordered).disabled(!UIImagePickerController.isSourceTypeAvailable(.camera))
        PhotosPicker(selection: $selection, maxSelectionCount: max(1, 5 - viewModel.draft.photos.count), matching: .images, preferredItemEncoding: .current) {
            Label("Add photos", systemImage: "photo.on.rectangle").frame(minHeight: 44)
        }.buttonStyle(.bordered)
    }

    private func importSelection() async {
        guard !selection.isEmpty else { return }
        let items = selection
        await viewModel.importPhotos {
            var dataItems: [Data] = []
            for item in items {
                try Task.checkCancellation()
                guard let data = try await item.loadTransferable(type: Data.self) else { throw BlockerFormError.invalid("This photo could not be loaded. Try another image.") }
                dataItems.append(data)
            }
            return dataItems
        }
        selection = []
    }

    private func takePhoto() {
        let status = AVCaptureDevice.authorizationStatus(for: .video)
        guard status != .denied && status != .restricted else {
            cameraMessage = "Camera access is unavailable. Add a photo from your library, or allow camera access in Settings."
            return
        }
        cameraMessage = nil
        showCamera = true
    }
}
