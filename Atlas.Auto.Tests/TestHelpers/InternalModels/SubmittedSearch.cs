using Atlas.Client.Models.Search.Requests;

namespace Atlas.Auto.Tests.TestHelpers.InternalModels;

/// <summary>
/// A search the test sent: what search tracking should have stored about it comes from here.
/// </summary>
/// <param name="Request">The request as the test sent it, before the public API filled in its defaults.</param>
/// <param name="SubmittedAtUtc">Test machine time just before the try that the public API accepted.</param>
internal record SubmittedSearch(string SearchIdentifier, SearchRequest Request, DateTime SubmittedAtUtc);
