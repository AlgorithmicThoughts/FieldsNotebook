using DataContract.Domain.Activity;
using DataContract.Domain.Delays;
using DataContract.Domain.Insights;
using DataContract.Domain.WorkItems;

namespace Engines.Delays;

/// <summary>
/// How active and past delays read from a crew's point of view: priority, attention, blocked work and
/// history. Every view is decided here and mapped to a client contract by the Manager.
/// </summary>
public interface IDelayInsightEngine : IEngine
{
    /// <summary>Home shows only the latest handovers; a full timeline belongs to the delay or activity screens.</summary>
    int RecentActivityLimit { get; }

    /// <summary>
    /// Midnight that ends the next project day, in UTC. Work planned before it should be under way by the
    /// end of the next project day, including work an earlier day already passed over.
    /// </summary>
    DateTimeOffset PlannedWorkCutoff(DateTimeOffset now);

    HomeView BuildHome(IReadOnlySet<int> ourCrewIds, ActiveDelaySet active, IReadOnlyList<ActivityEventRecord> activity);

    /// <summary>The unresolved blockers on planned work, earliest planned start first.</summary>
    IReadOnlyList<BlockedWorkView> BuildUpcoming(ActiveDelaySet active, PlannedWorkSet planned);

    IReadOnlyList<TrackingDelayView> BuildTracking(TrackingSet data);
}
