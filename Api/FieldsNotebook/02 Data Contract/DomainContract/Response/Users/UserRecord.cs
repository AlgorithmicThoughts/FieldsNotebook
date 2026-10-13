namespace DataContract.Domain.Users;

public sealed record UserRecord(
    int Id,
    string DisplayName,
    string Email,
    string? Phone,
    bool IsActive);
