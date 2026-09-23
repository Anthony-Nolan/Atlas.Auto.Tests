using Atlas.Auto.Tests.TestHelpers.Services;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class RequestValidationFailuresAssertions
    {
        public static void ShouldContain(this IReadOnlyCollection<ValidationFailureResponse> validationFailures, string validationFailure)
        {
            validationFailures.Should().NotBeNullOrEmpty();
            validationFailures.Should().Contain(failure => failure.ErrorMessage == validationFailure);
        }
    }
}
