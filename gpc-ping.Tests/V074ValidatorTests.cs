using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using gpc_ping.Validators;
using Shouldly;


namespace gpc;

public class V074ValidatorTests
{
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
}