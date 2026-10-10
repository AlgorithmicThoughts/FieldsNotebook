import Observation

@MainActor
@Observable
final class SessionViewModel {
    private(set) var isSignedIn = false

    func signIn() { isSignedIn = true }
    func signOut() { isSignedIn = false }
}
