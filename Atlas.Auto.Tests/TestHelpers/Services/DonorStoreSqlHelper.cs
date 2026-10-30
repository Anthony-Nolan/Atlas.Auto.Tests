using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace Atlas.Auto.Tests.TestHelpers.Services;

internal interface IDonorStoreSqlHelper
{
    Task<DonorCheckResult<Donor>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes);
    Task<DonorCheckResult<MatchingAlgorithmDonor>> CheckDonorsInMatchingAlgorithm(IEnumerable<string> externalDonorCodes);
    Task<IEnumerable<string>> GetExternalDonorCodesByRegistry(string registryCode, DateTimeOffset updatedBefore);
    Task<IReadOnlyCollection<string>> GetSearchableMatchingDonorCodesByRegistry(string registryCode);
    Task DeleteDonorsFromDonorStore(IEnumerable<string> externalDonorCodes);
    Task SetDonorsAsUnavailableForSearch(IEnumerable<string> externalDonorCodes);
    Task<IReadOnlyCollection<DonorImportFailure>> GetDonorImportFailuresByFileName(string fileName);
    Task<DonorImportHistoryRecord?> GetDonorImportHistoryByFileName(string fileName);
    Task<IReadOnlyCollection<DonorLog>> GetDonorLogs(IEnumerable<string> externalDonorCodes);
    Task<IReadOnlyCollection<PublishableDonorUpdate>> GetPublishableDonorUpdates(IEnumerable<int> donorIds);
    Task<IReadOnlyCollection<DonorManagementLog>> GetDonorManagementLogs(IEnumerable<int> donorIds);
    Task<IReadOnlyCollection<PrecomputedGenotypeSet>> GetPrecomputedGenotypeSets(int donorId);
    Task<int?> GetActiveHaplotypeFrequencySetId(string registryCode, string ethnicityCode);
}

internal class DonorStoreSqlHelper(
    IDbContextFactory<AtlasContext> atlasFactory,
    IDbContextFactory<AtlasMatchingContext> matchingFactory) : IDonorStoreSqlHelper
{
    public async Task<DonorCheckResult<Donor>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.Distinct().ToList();
        await using var ctx = atlasFactory.CreateDbContext();

        var donors = await ctx.Donors
            .Where(d => codes.Contains(d.ExternalDonorCode))
            .ToListAsync();

        return DonorCheckResult<Donor>.Build(codes, donors, d => d.ExternalDonorCode);
    }

    public async Task<DonorCheckResult<MatchingAlgorithmDonor>> CheckDonorsInMatchingAlgorithm(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.Distinct().ToList();
        await using var ctx = matchingFactory.CreateDbContext();
        var donors = await ctx.Donors
            .Where(d => codes.Contains(d.ExternalDonorCode) && d.IsAvailableForSearch)
            .ToListAsync();

        return DonorCheckResult<MatchingAlgorithmDonor>.Build(codes, donors, d => d.ExternalDonorCode);
    }

    public async Task<IEnumerable<string>> GetExternalDonorCodesByRegistry(string registryCode, DateTimeOffset updatedBefore)
    {
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.Donors
            .Where(d => d.RegistryCode == registryCode && d.LastUpdated < updatedBefore)
            .Select(d => d.ExternalDonorCode)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<string>> GetSearchableMatchingDonorCodesByRegistry(string registryCode)
    {
        var donorTypes = Enum.GetValues<MatchingDonorType>();
        await using var ctx = matchingFactory.CreateDbContext();
        // DonorType too, so the query can seek on the (DonorType, RegistryCode) index
        return await ctx.Donors
            .Where(d => donorTypes.Contains(d.DonorType) && d.RegistryCode == registryCode && d.IsAvailableForSearch)
            .Select(d => d.ExternalDonorCode)
            .ToListAsync();
    }

    public async Task DeleteDonorsFromDonorStore(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        await using var ctx = atlasFactory.CreateDbContext();
        await ctx.Donors
            .Where(d => codes.Contains(d.ExternalDonorCode))
            .ExecuteDeleteAsync();
    }

    public async Task SetDonorsAsUnavailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        await using var ctx = matchingFactory.CreateDbContext();
        await ctx.Donors
            .Where(d => codes.Contains(d.ExternalDonorCode) && d.IsAvailableForSearch)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsAvailableForSearch, false));
    }

    public async Task<IReadOnlyCollection<DonorImportFailure>> GetDonorImportFailuresByFileName(string fileName)
    {
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.DonorImportFailures
            .Where(f => f.UpdateFile != null && f.UpdateFile.EndsWith(fileName))
            .ToListAsync();
    }

    public async Task<DonorImportHistoryRecord?> GetDonorImportHistoryByFileName(string fileName)
    {
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.DonorImportHistory
            .Where(h => h.Filename.EndsWith(fileName))
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyCollection<DonorLog>> GetDonorLogs(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.DonorLogs
            .Where(l => codes.Contains(l.ExternalDonorCode))
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<PublishableDonorUpdate>> GetPublishableDonorUpdates(IEnumerable<int> donorIds)
    {
        var ids = donorIds.ToList();
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.PublishableDonorUpdates
            .Where(u => ids.Contains(u.DonorId))
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<DonorManagementLog>> GetDonorManagementLogs(IEnumerable<int> donorIds)
    {
        var ids = donorIds.ToList();
        await using var ctx = matchingFactory.CreateDbContext();
        return await ctx.DonorManagementLogs
            .Where(l => ids.Contains(l.DonorId))
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<PrecomputedGenotypeSet>> GetPrecomputedGenotypeSets(int donorId)
    {
        await using var ctx = matchingFactory.CreateDbContext();
        return await ctx.DonorSubjectGenotypeSets
            .Where(l => l.DonorId == donorId)
            .Join(ctx.SubjectGenotypeSetValues, l => l.SubjectGenotypeSetValueId, v => v.Id,
                (l, v) => new PrecomputedGenotypeSet(
                    l.AllowedLociKey,
                    v.AllowedLociKey,
                    v.HlaTypingKey,
                    v.HaplotypeFrequencySetId,
                    v.IsUnrepresented,
                    v.SubjectGenotypeSetData != null))
            .ToListAsync();
    }

    public async Task<int?> GetActiveHaplotypeFrequencySetId(string registryCode, string ethnicityCode)
    {
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.HaplotypeFrequencySets
            .Where(s => s.Active && s.RegistryCode == registryCode && s.EthnicityCode == ethnicityCode)
            .Select(s => (int?)s.Id)
            .SingleOrDefaultAsync();
    }
}
