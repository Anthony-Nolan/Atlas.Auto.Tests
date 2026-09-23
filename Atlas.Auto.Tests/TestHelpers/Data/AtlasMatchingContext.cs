using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class AtlasMatchingContext(DbContextOptions<AtlasMatchingContext> options) : DbContext(options)
{
    public DbSet<MatchingAlgorithmDonor> Donors { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
