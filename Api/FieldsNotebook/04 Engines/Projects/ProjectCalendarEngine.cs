namespace Engines.Projects
{
    /// <summary>The site clock every project runs on. Stored timestamps stay UTC; only day boundaries use this zone.</summary>
    public static class ProjectCalendarEngine
    {
        public static readonly TimeZoneInfo ProjectTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

        /// <summary>
        /// Midnight that ends the next project day, in UTC. Work planned before it is work that should be
        /// under way by the end of the next project day, including work an earlier day already passed over.
        /// </summary>
        public static DateTimeOffset EndOfNextProjectDay(DateTimeOffset now)
        {
            var dayAfterNext = TimeZoneInfo.ConvertTime(now, ProjectTimeZone).Date.AddDays(2);

            return new DateTimeOffset(dayAfterNext, ProjectTimeZone.GetUtcOffset(dayAfterNext)).ToUniversalTime();
        }
    }
}
