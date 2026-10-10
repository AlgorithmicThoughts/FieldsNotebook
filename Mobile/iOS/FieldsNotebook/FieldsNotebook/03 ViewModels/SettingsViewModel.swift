import Foundation
import Observation

@MainActor
@Observable
final class SettingsViewModel {
    var emailEnabled: Bool { didSet { savePreferences() } }
    var smsEnabled: Bool { didSet { savePreferences() } }
    @ObservationIgnored private let service: any WorkspaceProviding

    init(service: any WorkspaceProviding) {
        self.service = service
        let preferences = service.preferences()
        emailEnabled = preferences.emailEnabled
        smsEnabled = preferences.smsEnabled
    }

    private func savePreferences() {
        service.savePreferences(WorkspacePreferences(emailEnabled: emailEnabled, smsEnabled: smsEnabled))
    }
}
