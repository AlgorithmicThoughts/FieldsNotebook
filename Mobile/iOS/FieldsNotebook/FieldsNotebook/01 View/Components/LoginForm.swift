import SwiftUI

struct LoginForm: View {
    @Bindable var viewModel: LoginViewModel
    @FocusState private var focusedField: Field?
    private enum Field { case userID, password }

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            VStack(alignment: .leading, spacing: 8) {
                Text("Welcome back").font(.title2.weight(.bold))
                Text("Sign in to your field workspace.")
                    .font(.subheadline).foregroundStyle(NotebookStyle.muted)
            }
            fields
            if let message = viewModel.errorMessage {
                Label(message, systemImage: "exclamationmark.circle")
                    .font(.subheadline).foregroundStyle(NotebookStyle.urgent)
                    .accessibilityLabel("Sign in error: \(message)")
            }
            Button(action: signIn) {
                HStack { Text("Sign in"); Spacer(); Image(systemName: "arrow.right") }
                    .frame(maxWidth: .infinity)
            }
            .buttonStyle(NotebookPrimaryButton())
            Text("Demo access · User ID admin · Password admin")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
                .frame(maxWidth: .infinity, alignment: .center)
        }
        .padding(28).notebookCard()
        .onChange(of: viewModel.userID) { _, _ in viewModel.clearError() }
        .onChange(of: viewModel.password) { _, _ in viewModel.clearError() }
    }

    private var fields: some View {
        VStack(alignment: .leading, spacing: 18) {
            LoginField(label: "User ID") {
                TextField("Enter your user ID", text: $viewModel.userID)
                    .textContentType(.username).textInputAutocapitalization(.never)
                    .autocorrectionDisabled().focused($focusedField, equals: .userID)
                    .submitLabel(.next).onSubmit { focusedField = .password }
            }
            LoginField(label: "Password") {
                SecureField("Enter your password", text: $viewModel.password)
                    .textContentType(.password).focused($focusedField, equals: .password)
                    .submitLabel(.go).onSubmit(signIn)
            }
        }
    }

    private func signIn() {
        viewModel.signIn()
        if viewModel.errorMessage == nil { focusedField = nil }
    }
}
