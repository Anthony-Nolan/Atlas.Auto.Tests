namespace Atlas.Auto.Tests.TestHelpers.Data;

/// <summary>
/// The "Precomputed genotype sets used" events of one search, summed over its match prediction batches.
/// </summary>
internal record PrecomputedGenotypeSetUsage(
    long EventCount,
    string PrecomputeModes,
    string UsePrecomputedGenotypeSets,
    long DonorCount,
    long PrecomputedCount);
