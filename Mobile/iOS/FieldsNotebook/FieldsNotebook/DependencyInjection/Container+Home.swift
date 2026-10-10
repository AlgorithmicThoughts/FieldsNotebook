import FactoryKit

extension Container {
    @MainActor
    var homeService: Factory<any HomeProviding> {
        self { @MainActor in HomeMock() }
    }

    @MainActor
    var homeViewModel: Factory<HomeViewModel> {
        self { @MainActor in HomeViewModel(service: self.homeService()) }
    }

    @MainActor
    var notebookService: Factory<any NotebookProviding> {
        self { @MainActor in NotebookMock(homeService: self.homeService()) }
    }

    @MainActor
    var trackingViewModel: Factory<TrackingViewModel> {
        self { @MainActor in TrackingViewModel(service: self.notebookService()) }
    }

    @MainActor
    var documentsViewModel: Factory<DocumentsViewModel> {
        self { @MainActor in DocumentsViewModel(service: self.notebookService()) }
    }

    @MainActor
    var upcomingViewModel: Factory<UpcomingViewModel> {
        self { @MainActor in UpcomingViewModel(service: self.notebookService()) }
    }

    @MainActor
    var loginService: Factory<any LoginProviding> {
        self { @MainActor in LoginMock() }
    }

    @MainActor
    var sessionViewModel: Factory<SessionViewModel> {
        self { @MainActor in SessionViewModel() }.shared
    }

    @MainActor
    var loginViewModel: Factory<LoginViewModel> {
        self { @MainActor in LoginViewModel(service: self.loginService(), session: self.sessionViewModel()) }
    }
}
