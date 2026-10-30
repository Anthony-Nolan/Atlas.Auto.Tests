namespace Atlas.Auto.Tests.TestHelpers.Data;

/// <summary>
/// The "Precomputed genotype sets used" events of one search, summed over its match prediction batches.
/// </summary>
/// <param name="DonorCount">Donor inputs: donors with the same typing and frequency set metadata are one input.</param>
/// <param name="DonorIdCount">Donors.</param>
/// <param name="PrecomputedCount">Donor inputs read from a precomputed genotype set.</param>
internal record PrecomputedGenotypeSetUsage(
    long EventCount,
    string PrecomputeModes,
    string UsePrecomputedGenotypeSets,
    long DonorCount,
    long DonorIdCount,
    long PrecomputedCount);
