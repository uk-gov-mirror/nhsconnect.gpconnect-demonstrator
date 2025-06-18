using gpc_ping.Validators;
using Shouldly;
using static gpc.Helpers.TestHelpers;

namespace gpc_ping.Tests;

public class V160ValidatorTests
{
    # region Reason for Request

    [Theory]
    [InlineData("directcare")]
    [InlineData("migration")]
    public void ValidateReasonForRequest_ShouldReturnTrue_WhenValidValue(string reason)
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(new Dictionary<string, string>
        {
            { "reason_for_request", reason }
        });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        Assert.True(result.IsValid);
        result.Message.ShouldBe("'reason_for_request' is valid");
    }

    [Theory]
    [InlineData("invalid_reason")]
    [InlineData("reason1")]
    [InlineData("directcare migration")]
    public void ValidateReasonForRequest_ShouldReturnFalse_WhenInvalidValue(string reason)
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string>
            {
                {
                    "reason_for_request", reason
                }
            });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe($"Invalid 'reason_for_request': '{reason}'");
    }


    [Fact]
    public void ValidateReasonForRequest_ShouldReturnFalse_WhenEmpty()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string>
            {
                {
                    "reason_for_request", string.Empty
                }
            });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Missing 'reason_for_request' claim");
    }

    # endregion

    #region ValidateRequestedScope

    [Fact]
    public void ValidateRequestedScope_ShouldReturnValid_WhenScopeOnlyIsAccepted()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "scope1" } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("'requested_scope' claim is valid");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnValid_WhenScopeAndConfidentialityValid()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "scope1 conf/N" } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("'requested_scope' claim is valid");
    }


    [Fact]
    public void ValidateRequestedScope_ShouldThrow_WhenAcceptedClaimValuesAreNull()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "scope1" } });

        // Act & Assert
        var exception =
            Should.Throw<ArgumentException>(() => validator.ValidateRequestedScope(null));
        exception.ParamName.ShouldBe("acceptedClaimValues");
        exception.Message.ShouldContain("acceptedClaims must not be null or empty");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldThrow_WhenAcceptedClaimValuesAreEmpty()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "scope1" } });

        // Act & Assert
        var exception =
            Should.Throw<ArgumentException>(() => validator.ValidateRequestedScope([]));
        exception.ParamName.ShouldBe("acceptedClaimValues");
        exception.Message.ShouldContain("acceptedClaims must not be null or empty");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnFalse_WhenRequestedScopeClaimIsMissing()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string>());

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Missing 'requested_scope' claim");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnFalse_WhenRequestedScopeClaimIsEmpty()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "" } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'requested_scope' claim cannot be null or empty");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnFalse_WhenRequestedScopeClaimHasNoValues()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "   " } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'requested_scope' claim cannot be null or empty");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnFalse_WhenScopeIsInvalid()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "invalidScope" } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid 'requested_scope' claim - claim contains 1 invalid value(s)");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnFalse_WhenConfidentialityIsInvalid()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "scope1 conf/X" } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid 'requested_scope' claim - claim contains 2 invalid value(s)");
    }

    [Fact]
    public void ValidateRequestedScope_ShouldReturnFalse_WhenMoreThanTwoClaimValues()
    {
        // Arrange
        var validator = CreateValidatorWithToken<V160Validator>(
            new Dictionary<string, string> { { "requested_scope", "scope1 conf/N extra" } });

        // Act
        var result = validator.ValidateRequestedScope(["scope1"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("requested_scope claim is  invalid");
    }

    #endregion
}