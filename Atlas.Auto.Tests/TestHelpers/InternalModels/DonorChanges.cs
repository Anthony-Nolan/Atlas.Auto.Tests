namespace Atlas.Auto.Tests.TestHelpers.InternalModels
{
    internal class DonorChanges
    {
        public required IReadOnlyCollection<string> NoLongerMatching { get; set; }
        public required IReadOnlyCollection<string> NewlyMatching { get; set; }
    }
}
