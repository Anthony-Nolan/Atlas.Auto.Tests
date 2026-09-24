using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace Atlas.Auto.Tests.TestHelpers.Services;

internal interface IDonorStoreSqlHelper
{
    Task<DonorCheckResult<Donor>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes);
    Task<DonorCheckResult<MatchingAlgorithmDonor>> CheckDonorsInMatchingAlgorithm(IEnumerable<string> externalDonorCodes);
    Task<IEnumerable<string>> GetExternalDonorCodesByRegistry(string registryCode, DateTimeOffset updatedBefore);
    Task DeleteDonorsFromDonorStore(IEnumerable<string> externalDonorCodes);
    Task SetDonorsAsUnavailableForSearch(IEnumerable<string> externalDonorCodes);
    Task<IReadOnlyCollection<DonorImportFailure>> GetDonorImportFailuresByFileName(string fileName);
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
}
