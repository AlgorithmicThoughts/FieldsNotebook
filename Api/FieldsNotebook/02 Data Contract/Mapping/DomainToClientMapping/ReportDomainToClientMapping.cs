using DataContract.Domain.Reports;
using DataContract.Reports;

namespace DataContract.Mapping.DomainToClientMapping;

public static class ReportDomainToClientMapping
{
    /// <summary>One row per delay, each naming every crew that delay blocks.</summary>
    public static IReadOnlyList<DelayReportRowResponse> ToResponse(this DelayReportSet report)
    {
        var blockedCrews = report.Impacts.MapAll(i => i).ToLookup(i => i.DelayId, i => i.CrewName);
        return report.Delays.MapAll(d => d.ToResponse(blockedCrews[d.DelayId].ToList()));
    }

    public static DelayReportRowResponse ToResponse(this DelayReportRecord delay, IEnumerable<string>? blockedCrewNames)
    {
        return new DelayReportRowResponse(delay.Number, delay.ProjectName, delay.Title, delay.StatusName, delay.TagName,
            delay.ReportType, delay.OwnerCrewName, blockedCrewNames.MapAll(name => name), delay.SegmentName,
            delay.LocationName, delay.ReportedByName, delay.CreatedAt, delay.FinalCycleClosedAt, delay.CycleCount, delay.PhotoCount);
    }
}
