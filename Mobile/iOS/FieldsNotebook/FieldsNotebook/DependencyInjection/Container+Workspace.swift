import FactoryKit

extension Container {
    @MainActor
    var reportsService: Factory<any ReportsProviding> {
        self { @MainActor in ReportsMock(homeService: self.homeService(), notebookService: self.notebookService()) }
    }

    @MainActor
    var reportsViewModel: Factory<ReportsViewModel> {
        self { @MainActor in ReportsViewModel(service: self.reportsService()) }
    }

    @MainActor
    var workspaceService: Factory<any WorkspaceProviding> {
        self { @MainActor in WorkspaceMock(homeService: self.homeService(), notebookService: self.notebookService()) }.shared
    }

    @MainActor
    var settingsViewModel: Factory<SettingsViewModel> {
        self { @MainActor in SettingsViewModel(service: self.workspaceService()) }.shared
    }

    @MainActor
    var notificationsViewModel: Factory<NotificationsViewModel> {
        self { @MainActor in NotificationsViewModel(service: self.workspaceService()) }.shared
    }
}
