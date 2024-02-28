using Atlas.Auto.Tests.TestHelpers.InternalModels;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions;

internal static class DebugResponseAssertions
{
    public static void ShouldBeSuccessful<T>(this DebugResponse<T> response)
    {
        response.Should().NotBeNull();
        response.WasSuccess.Should().BeTrue();
    }
}