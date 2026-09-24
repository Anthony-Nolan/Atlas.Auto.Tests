namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class DonorCheckResult<T>
{
    public required IReadOnlyCollection<T> PresentDonors { get; init; }
    public required IReadOnlyCollection<string> AbsentDonors { get; init; }
    public int PresentCount => PresentDonors.Count;
    public int AbsentCount => AbsentDonors.Count;

    public static DonorCheckResult<T> Build(
        IReadOnlyCollection<string> requestedCodes,
        IReadOnlyCollection<T> foundDonors,
        Func<T, string> getCode)
    {
        var presentCodes = foundDonors.Select(getCode).ToHashSet();
        var absentCodes = requestedCodes.Where(c => !presentCodes.Contains(c)).ToList();

        return new DonorCheckResult<T>
        {
            PresentDonors = foundDonors,
            AbsentDonors = absentCodes
        };
    }
}
