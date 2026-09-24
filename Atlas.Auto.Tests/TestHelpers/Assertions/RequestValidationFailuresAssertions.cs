using Atlas.Auto.Tests.TestHelpers.Services;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class RequestValidationFailuresAssertions
    {
        public static void ShouldContain(this IReadOnlyCollection<ValidationFailureResponse> validationFailures, string validationFailure)
        {
            using (new AssertionScope())
            {
                validationFailures.Should().NotBeNullOrEmpty();
                validationFailures.Should().Contain(failure => failure.ErrorMessage == validationFailure);
            }
        }
    }
}
