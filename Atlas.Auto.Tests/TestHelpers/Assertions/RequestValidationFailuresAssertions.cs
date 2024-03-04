using Atlas.Debug.Client.Models.Validation;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class RequestValidationFailuresAssertions
    {
        public static void ShouldContain(this IReadOnlyCollection<RequestValidationFailure> validationFailures, string validationFailure)
        {
            validationFailures.Should().NotBeNullOrEmpty();
            validationFailures.Should().Contain(failure => failure.ErrorMessage == validationFailure);
        }
    }
}
