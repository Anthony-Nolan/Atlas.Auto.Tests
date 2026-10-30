using Atlas.Auto.Tests.TestHelpers.Data.Entities;

namespace Atlas.Auto.Tests.TestHelpers.Data;

/// <summary>
/// A donor's link row joined to its value row. The payload itself is not read, only whether it is there.
/// </summary>
internal record PrecomputedGenotypeSet(
    AllowedLociKey AllowedLociKey,
    AllowedLociKey ValueAllowedLociKey,
    string HlaTypingKey,
    int HaplotypeFrequencySetId,
    bool IsUnrepresented,
    bool HasData);
