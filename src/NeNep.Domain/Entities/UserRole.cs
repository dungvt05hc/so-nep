using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// A person may hold several roles; a class-officer role is tied to one class.
/// </summary>
public class UserRole
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public Role Role { get; set; }

    /// <summary>null for ADMIN and BGH.</summary>
    public int? ClassId { get; set; }

    public User User { get; set; } = null!;
}
