using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Reads the SearchTracking tables of the shared Atlas database.
/// </summary>
internal class SearchTrackingSqlHelper(IDbContextFactory<AtlasContext> atlasFactory)
{
    /// <param name="searchIdentifier">Search or repeat search ID, as the public API returned it.</param>
    public async Task<SearchTrackingRequest?> GetSearchRequest(string searchIdentifier)
    {
        // Search tracking stores GUIDs in lower case
        var identifier = searchIdentifier.ToLowerInvariant();
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.SearchTrackingRequests.SingleOrDefaultAsync(r => r.SearchIdentifier == identifier);
    }

    public async Task<IReadOnlyCollection<SearchTrackingMatchingAlgorithmAttempt>> GetMatchingAlgorithmAttempts(int searchRequestId)
    {
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.SearchTrackingMatchingAlgorithmAttempts
            .Where(a => a.SearchRequestId == searchRequestId)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<SearchTrackingMatchPrediction>> GetMatchPredictions(int searchRequestId)
    {
        await using var ctx = atlasFactory.CreateDbContext();
        return await ctx.SearchTrackingMatchPredictions
            .Where(p => p.SearchRequestId == searchRequestId)
            .ToListAsync();
    }
}
