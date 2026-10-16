using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verus.Domain.Companies;

namespace Verus.Infrastructure.Persistence;

/// <summary>
/// Maps the aggregate without touching the domain model. Value objects are owned types, so a relational
/// provider stores them as columns of the Companies table (Name, WebsiteUrl, WebsiteHost, WebsiteDomain…).
/// </summary>
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasConversion(id => id.Value, value => new CompanyId(value)).ValueGeneratedNever();

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.OwnsOne(
            c => c.Name,
            name =>
            {
                name.Property(n => n.Value).HasColumnName("Name").HasMaxLength(CompanyName.MaxLength).IsRequired();
            }
        );
        builder.Navigation(c => c.Name).IsRequired();

        builder.OwnsOne(
            c => c.Website,
            website =>
            {
                website
                    .Property(w => w.Value)
                    .HasColumnName("WebsiteUrl")
                    .HasMaxLength(WebsiteUrl.MaxLength)
                    .IsRequired();
                website.Property(w => w.Host).HasColumnName("WebsiteHost").HasMaxLength(253).IsRequired();
                website
                    .Property(w => w.RegistrableDomain)
                    .HasColumnName("WebsiteDomain")
                    .HasMaxLength(253)
                    .IsRequired();
                website.Property(w => w.DomainLabel).HasColumnName("WebsiteDomainLabel").HasMaxLength(63).IsRequired();

                // Enforced by relational providers; the InMemory provider ignores it (the handler checks too).
                website.HasIndex(w => w.RegistrableDomain).IsUnique();
                website.HasIndex(w => w.Host);
            }
        );
        builder.Navigation(c => c.Website).IsRequired();
    }
}
