import Foundation
import Observation

@MainActor
@Observable
final class LoginViewModel {
    var userID = ""
    var password = ""
    private(set) var errorMessage: String?
    @ObservationIgnored private let service: any LoginProviding
    @ObservationIgnored private let session: SessionViewModel

    init(service: any LoginProviding, session: SessionViewModel) {
        self.service = service
        self.session = session
    }

    func signIn() {
        guard !userID.isEmpty && !password.isEmpty else {
            errorMessage = "Enter your user ID and password."
            return
        }
        guard service.accepts(userID: userID, password: password) else {
            errorMessage = "The user ID or password is incorrect."
            return
        }
        errorMessage = nil
        password = ""
        session.signIn()
    }

    func clearError() { errorMessage = nil }
}
