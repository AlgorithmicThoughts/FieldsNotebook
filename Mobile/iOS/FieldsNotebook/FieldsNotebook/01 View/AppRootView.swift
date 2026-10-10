import SwiftUI
import FactoryKit

struct AppRootView: View {
    @InjectedObservable(\.sessionViewModel) private var session

    var body: some View {
        Group {
            if session.isSignedIn {
                ContentView(onSignOut: session.signOut)
            } else {
                LoginView()
            }
        }
        .tint(NotebookStyle.accent)
        .preferredColorScheme(.light)
    }
}
