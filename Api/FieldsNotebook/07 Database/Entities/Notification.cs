namespace Database.Entities
{
    /// <summary>Maps dbo.Notifications. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
    public partial class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int DelayId { get; set; }
        public int ActivityEventId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ReadAt { get; set; }
    }
}
