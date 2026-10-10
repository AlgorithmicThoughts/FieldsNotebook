import FactoryKit

extension Container {
    @MainActor
    var reportBlockerService: Factory<any ReportBlockerProviding> {
        self { @MainActor in ReportBlockerMock() }.shared
    }

    var blockerPhotoLoader: Factory<BlockerPhotoLoader> { self { BlockerPhotoLoader() } }

    @MainActor
    var reportBlockerViewModel: ParameterFactory<(HomeProject, HomeUser), ReportBlockerViewModel> {
        self { @MainActor project, user in
            ReportBlockerViewModel(project: project, user: user, service: self.reportBlockerService(), photoLoader: self.blockerPhotoLoader())
        }
    }
}
