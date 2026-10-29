using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class AtlasMatchingContext(DbContextOptions<AtlasMatchingContext> options) : DbContext(options)
{
    public DbSet<MatchingAlgorithmDonor> Donors { get; set; } = null!;
    public DbSet<DonorManagementLog> DonorManagementLogs { get; set; } = null!;
    public DbSet<DonorSubjectGenotypeSet> DonorSubjectGenotypeSets { get; set; } = null!;
    public DbSet<SubjectGenotypeSetValue> SubjectGenotypeSetValues { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Atlas stores the key as its member name, not its int value
        modelBuilder.Entity<DonorSubjectGenotypeSet>().Property(x => x.AllowedLociKey).HasConversion<string>();
        modelBuilder.Entity<SubjectGenotypeSetValue>().Property(x => x.AllowedLociKey).HasConversion<string>();
    }
}
