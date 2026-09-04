using System.Text;
using Npgsql;

namespace NeNep.Infrastructure.Persistence;

/// <summary>
/// Naming rules for the PostgreSQL enum types.
/// <para>
/// TYPE names are converted to snake_case to match the naming convention used across
/// the whole schema (<c>Role</c> becomes <c>role</c>, <c>ViolationStatus</c> becomes
/// <c>violation_status</c>).
/// </para>
/// <para>
/// LABEL names are kept exactly as they appear in <c>docs/schema.prisma</c>
/// (<c>ADMIN</c>, <c>LOP_TRUONG</c>, <c>PENDING</c>...). An enum label is DATA: it shows
/// up in seed scripts, in reports and in hand-written queries, so its casing must never
/// be rewritten.
/// </para>
/// </summary>
public sealed class NeNepEnumNameTranslator : INpgsqlNameTranslator
{
    public static readonly NeNepEnumNameTranslator Instance = new();

    private NeNepEnumNameTranslator()
    {
    }

    public string TranslateTypeName(string clrName) => ToSnakeCase(clrName);

    public string TranslateMemberName(string clrName) => clrName;

    private static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c) && i > 0 && !char.IsUpper(name[i - 1]))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
