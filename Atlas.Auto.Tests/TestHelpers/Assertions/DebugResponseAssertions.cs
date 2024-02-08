using Atlas.Auto.Tests.TestHelpers.InternalModels;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions;

internal static class DebugResponseAssertions
{
    public static void ShouldBeSuccessful<T>(this DebugResponse<T> response)
    {
        response.Should().NotBeNull("the debug response should not be null");
        response.WasSuccess.Should().BeTrue("the debug response should have been successful");
    }
}