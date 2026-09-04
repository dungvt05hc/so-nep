using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>Catalog group for violation and commendation codes.</summary>
public class ViolationCategory
{
    public int Id { get; set; }

    public CategoryKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public bool IsActive { get; set; }

    public ICollection<ViolationType> Types { get; set; } = new List<ViolationType>();
}
