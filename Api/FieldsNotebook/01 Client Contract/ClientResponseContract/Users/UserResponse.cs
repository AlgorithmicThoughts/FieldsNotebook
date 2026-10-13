namespace DataContract.Users;

public sealed record UserResponse(
    int Id,
    string DisplayName,
    string Email,
    string? Phone,
    bool IsActive);
