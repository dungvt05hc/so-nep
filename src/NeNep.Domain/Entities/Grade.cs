namespace NeNep.Domain.Entities;

/// <summary>A grade level.</summary>
public class Grade
{
    public int Id { get; set; }

    /// <summary>6, 7, 8 or 9.</summary>
    public int Level { get; set; }

    /// <summary>"Khối 6" — shown to users, so it stays in Vietnamese.</summary>
    public string Name { get; set; } = string.Empty;

    public ICollection<Class> Classes { get; set; } = new List<Class>();
}
