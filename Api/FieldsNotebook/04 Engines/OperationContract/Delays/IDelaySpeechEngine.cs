using DataContract.Domain.Delays;

namespace Engines.Delays;

/// <summary>Suggests editable report fields from a transcript, using the project's live crews and reasons.</summary>
public interface IDelaySpeechEngine : IEngine
{
    ReportSuggestionRecord SuggestFields(string transcript, ReportChoicesRecord choices);
}
