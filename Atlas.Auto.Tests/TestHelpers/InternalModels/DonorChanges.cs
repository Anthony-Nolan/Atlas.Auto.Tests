namespace Atlas.Auto.Tests.TestHelpers.InternalModels
{
    internal class DonorChanges
    {
        public IReadOnlyCollection<string> NoLongerMatching { get; set; }
        public IReadOnlyCollection<string> NewlyMatching { get; set; }
    }
}
