import FactoryKit

extension Container {
    @MainActor
    var reportBlockerService: Factory<any ReportBlockerProviding> {
        self { @MainActor in ReportBlockerMock() }.shared
    }

    var blockerPhotoLoader: Factory<BlockerPhotoLoader> { self { BlockerPhotoLoader() } }

    var speechTranscriber: Factory<any SpeechTranscribing> { self { SpeechTranscriptionService() } }
    var spokenBlockerFiller: Factory<any SpokenBlockerFilling> { self { SpokenBlockerFiller() } }

    @MainActor
    var blockerAudioRecorder: Factory<any BlockerAudioRecording> { self { @MainActor in BlockerAudioRecorder() } }

    @MainActor
    var blockerRecordingViewModel: Factory<BlockerRecordingViewModel> {
        self { @MainActor in
            BlockerRecordingViewModel(recorder: self.blockerAudioRecorder(), transcriber: self.speechTranscriber(), filler: self.spokenBlockerFiller())
        }
    }

    @MainActor
    var reportBlockerViewModel: ParameterFactory<(HomeProject, HomeUser), ReportBlockerViewModel> {
        self { @MainActor project, user in
            ReportBlockerViewModel(project: project, user: user, service: self.reportBlockerService(), photoLoader: self.blockerPhotoLoader(), recording: self.blockerRecordingViewModel())
        }
    }
}
