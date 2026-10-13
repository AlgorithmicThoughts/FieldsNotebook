using Utilities.OperationContract;

namespace Utilities.OperationImplementation;

public sealed class ClockUtility(TimeProvider timeProvider) : IClockUtility
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
