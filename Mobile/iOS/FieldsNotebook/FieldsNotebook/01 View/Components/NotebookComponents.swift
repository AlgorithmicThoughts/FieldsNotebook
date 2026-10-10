import SwiftUI

extension View {
    func notebookCard() -> some View {
        background(NotebookStyle.surface)
            .clipShape(RoundedRectangle(cornerRadius: 16))
            .overlay(RoundedRectangle(cornerRadius: 16).stroke(NotebookStyle.border, lineWidth: 1))
    }
}

struct NotebookPrimaryButton: ButtonStyle {
    @Environment(\.isEnabled) private var isEnabled

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.subheadline.weight(.semibold))
            .padding(.horizontal, 16)
            .frame(minHeight: 44)
            .foregroundStyle(isEnabled ? NotebookStyle.onAccent : NotebookStyle.disabledText)
            .background(background(isPressed: configuration.isPressed))
            .clipShape(RoundedRectangle(cornerRadius: 10))
    }

    private func background(isPressed: Bool) -> Color {
        guard isEnabled else { return NotebookStyle.disabled }
        return isPressed ? NotebookStyle.pressed : NotebookStyle.accent
    }
}
