namespace Utilities.OperationContract;

/// <summary>The current time, injected so time-based rules can be tested at a fixed instant.</summary>
public interface IClockUtility : IUtility
{
    DateTimeOffset UtcNow { get; }
}
