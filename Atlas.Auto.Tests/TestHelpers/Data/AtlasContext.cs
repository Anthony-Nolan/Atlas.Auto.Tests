using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class AtlasContext(DbContextOptions<AtlasContext> options) : DbContext(options)
{
    public DbSet<Donor> Donors { get; set; } = null!;
    public DbSet<DonorImportFailure> DonorImportFailures { get; set; } = null!;
    public DbSet<DataRefreshRecord> DataRefreshHistory { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
