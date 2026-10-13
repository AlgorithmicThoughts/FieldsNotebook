using DataContract.Domain.Reports;
using DataContract.Reports;

namespace DataContract.Mapping.ClientToDomainMapping;

public static class ReportClientToDomainMapping
{
    /// <summary>Inclusive report days become UTC bounds: the start of the first day up to the start of the day after the last.</summary>
    public static DelayReportQueryRecord ToDomain(this DelayReportRequest request)
    {
        return new DelayReportQueryRecord(
            request.ProjectIds.MapAll(id => id).Distinct().ToList(),
            StartOfUtcDay(request.DateFrom),
            StartOfUtcDay(request.DateTo?.AddDays(1)));
    }

    private static DateTimeOffset? StartOfUtcDay(DateOnly? day)
    {
        if (day is { } value)
        {
            return new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        return null;
    }
}
