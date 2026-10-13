namespace Database.Entities;

/// <summary>Maps dbo.UserProjects. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class UserProject
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public bool IsActive { get; set; }
}
