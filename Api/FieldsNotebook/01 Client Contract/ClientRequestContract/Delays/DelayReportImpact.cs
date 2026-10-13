using System.ComponentModel.DataAnnotations;

namespace DataContract.Delays;

public sealed class DelayReportImpact
{
    [Range(1, int.MaxValue)]
    public int BlockedCrewId { get; set; }

    public int? BlockedWorkItemId { get; set; }
}
