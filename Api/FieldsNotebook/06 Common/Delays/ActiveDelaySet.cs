namespace Common.Delays
{
    /// <summary>Active delays that involve a set of crews, with every impact of those delays.</summary>
    public sealed record ActiveDelaySet(IReadOnlyList<ActiveDelayRecord> Delays, IReadOnlyList<ActiveDelayImpactRecord> Impacts)
    {
        public static ActiveDelaySet Empty { get; } = new([], []);
    }

    /// <param name="LocationName">Delays.LocationName, or the segment name when none was typed.</param>
    /// <param name="CycleCount">All cycles of the delay; more than one means it was reopened.</param>
    /// <param name="ExpectedReadyAt">Tentative completion time of the open cycle, in UTC.</param>
    public sealed record ActiveDelayRecord(
        int DelayId,
        string Number,
        string Title,
        string Description,
        string StatusCode,
        string TagCode,
        string TagName,
        int OwnerCrewId,
        string OwnerCrewName,
        string ReportType,
        string LocationName,
        DateTimeOffset CreatedAt,
        int CycleCount,
        bool HasOpenCycle,
        DateTimeOffset? ExpectedReadyAt);

    /// <param name="IsVerifiedInOpenCycle">The affected crew's Foreman verified this impact in the open cycle.</param>
    public sealed record ActiveDelayImpactRecord(int DelayId, int BlockedCrewId, string BlockedCrewName, bool IsVerifiedInOpenCycle);
}
