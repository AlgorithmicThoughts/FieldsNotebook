namespace Database.Entities;

/// <summary>Maps dbo.UserRoles. Generated from Field-Notebook-Create-And-Seed.sql; add behavior in a separate partial file.</summary>
public partial class UserRole
{
    public int UserRoleId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int ProjectId { get; set; }
    public bool IsActive { get; set; }
}
