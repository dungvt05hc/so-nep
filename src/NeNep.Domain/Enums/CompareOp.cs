namespace NeNep.Domain.Enums;

/// <summary>Comparison between the observed count and a rule's threshold.</summary>
public enum CompareOp
{
    /// <summary>&gt; — "more than 10 times" means &gt; 10.</summary>
    GT,

    /// <summary>&gt;=</summary>
    GTE,
}
