import Foundation

@MainActor
protocol LoginProviding {
    func accepts(userID: String, password: String) -> Bool
}

/// Demo-only credentials. No persisted password or production authentication.
struct LoginMock: LoginProviding {
    func accepts(userID: String, password: String) -> Bool {
        userID == "admin" && password == "admin"
    }
}
