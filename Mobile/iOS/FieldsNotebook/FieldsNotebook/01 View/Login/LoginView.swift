import SwiftUI
import FactoryKit

struct LoginView: View {
    @InjectedObservable(\.loginViewModel) private var viewModel

    var body: some View {
        GeometryReader { geometry in
            ScrollView {
                ViewThatFits(in: .horizontal) {
                    HStack(alignment: .center, spacing: 64) {
                        LoginIntroView().frame(width: 400)
                        LoginForm(viewModel: viewModel).frame(width: 400)
                    }
                    VStack(alignment: .leading, spacing: 32) {
                        LoginIntroView()
                        LoginForm(viewModel: viewModel)
                    }
                    .frame(maxWidth: 440)
                }
                .padding(32)
                .frame(maxWidth: .infinity, minHeight: geometry.size.height)
            }
            .scrollDismissesKeyboard(.interactively)
            .background(NotebookStyle.bg)
        }
        .foregroundStyle(NotebookStyle.text)
        .tint(NotebookStyle.accent)
        .preferredColorScheme(.light)
    }
}

#Preview { LoginView() }
