namespace Database.Entities
{
    /// <summary>Maps dbo.Delays. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class Delay
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ReportedByUserId { get; set; }
        public int ReportedByCrewId { get; set; }
        public int OwnerCrewId { get; set; }
        public int DelayTagId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public int? BlockingWorkItemId { get; set; }
        public int DelayStatusId { get; set; }
        public int SegmentId { get; set; }
        public int? SiteMapId { get; set; }
        public decimal? PinX { get; set; }
        public decimal? PinY { get; set; }
        public string LocationSource { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
