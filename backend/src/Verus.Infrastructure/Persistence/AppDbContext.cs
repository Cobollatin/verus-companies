using Microsoft.EntityFrameworkCore;
using Verus.Domain.Companies;

namespace Verus.Infrastructure.Persistence;

/// <summary>EF Core unit of work. Internal: nothing outside Infrastructure sees storage types.</summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
