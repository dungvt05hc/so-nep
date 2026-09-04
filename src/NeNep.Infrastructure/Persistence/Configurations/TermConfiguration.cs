using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeNep.Domain.Entities;

namespace NeNep.Infrastructure.Persistence.Configurations;

public class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> builder)
    {
        builder.ToTable("terms");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.YearId, x.Code }).IsUnique();

        builder.HasOne(x => x.Year)
            .WithMany(x => x.Terms)
            .HasForeignKey(x => x.YearId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
