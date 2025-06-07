using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gpc_ping.Validators;
using Shouldly;


namespace gpc;

public class V074ValidatorTests
{
    # region Audience Tests

    [Fact]
    public void ValidateAudience_ValidAudience()
    {
        // Arrange
        const string tokenString =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJodHRwczovL2F1dGhvcml6ZS5maGlyLm5ocy5uZXQvdG9rZW4ifQ.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var validator = new V074Validator(token);

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("Audience is valid");
    }

    [Theory]
    [InlineData("eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJ0b2tlbiJ9.")] // no uri
    [InlineData(
        "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJodHRwczovL3RoaXJkcGFydHlwcm92aWRlci5uZXQifQ.")] // wrong uri
    public void ValidateAudience_InvalidAudience(string tokenString)
    {
        // Arrange
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var validator = new V074Validator(token);

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Audience is not valid - see GP Connect specification");
    }


    [Theory]
    [InlineData("Empty audience", "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiIifQ.")] // empty aud
    [InlineData("null", "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOm51bGx9.")] // null
    [InlineData("Whitespace", "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiIgICAifQ.")] // whitespace
    public void ValidateAudience_InvalidAudience_NullOrEmptyAudience(string reason, string? tokenString)
    {
        // Arrange
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var validator = new V074Validator(token);

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Audience is not valid - must have value");
    }

    #endregion

    #region Reason for request

    [Fact]
    public void ValidateReasonForRequest_ReturnsTrue_WhenValid()
    {
        // Arrange
        var validator = CreateValidatorWithToken(new Dictionary<string, string>()
        {
            { "reason_for_request", "directcare" }
        });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("Reason for request is valid.");
    }

    [Fact]
    public void ValidateReasonForRequest_ReturnsFalse_WhenMissingClaim()
    {
        // Arrange
        var validator = CreateValidatorWithToken(new Dictionary<string, string>());

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Missing 'reason_for_request' claim");
    }

    [Fact]
    public void ValidateReasonForRequest_ReturnsFalse_WhenInvalidClaim()
    {
        // Arrange
        var validator = CreateValidatorWithToken(new Dictionary<string, string>()
        {
            { "reason_for_request", "patient_data" }
        });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe($"Invalid reason for request: 'patient_data'");
    }

    #endregion


    private JwtSecurityToken CreateTokenWithClaims(Dictionary<string, string> claimsDict)
    {
        var claims = claimsDict.Select(kvp => new Claim(kvp.Key, kvp.Value)).ToList();

        return new JwtSecurityToken(claims: claims);
    }

    private V074Validator CreateValidatorWithToken(Dictionary<string, string> claims)
    {
        var token = CreateTokenWithClaims(claims);
        return new V074Validator(token);
    }
}