using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using gpc_ping;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

public class ValidatorTests
{
    #region Validate Header

    [Fact]
    public void ValidateHeader_ValidHeader()
    {
        // Arrange
        const string tokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJhdWRpZW5jZV90ZXN0In0.";
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var testValidator = new TestValidator(token);

        // Act
        var result = testValidator.ValidateHeader();

        // Assert
        result.Message.ShouldBe("Header is valid");
        result.IsValid.ShouldBeTrue();
    }


    [Theory]
    [InlineData("eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJhdWQiOiJhdWRpZW5jZV90ZXN0In0.")] // invalid alg
    [InlineData("eyJ0eXAiOiJpbnZhbGlkIiwiYWxnIjoibm9uZSJ9.eyJhdWQiOiJhdWRpZW5jZV90ZXN0In0.")] // invalid typ
    public void ValidateHeader_InvalidHeader(string tokenString)
    {
        // Arrange
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var testValidator = new TestValidator(token);

        // Act
        var result = testValidator.ValidateHeader();

        // Assert
        result.Message.ShouldBe("Header is invalid - check GP Connect specification");
        result.IsValid.ShouldBeFalse();
    }

    #endregion

    #region Validate Issuer

    [Fact]
    public void ValidateIssuer_ValidIssuer()
    {
        // Arrange
        const string tokenString =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJpc3MiOiJodHRwczovL2F1dGhvcml6ZS5maGlyLm5ocy5uZXQvdG9rZW4ifQ.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var testValidator = new TestValidator(token);

        // Act
        var result = testValidator.ValidateIssuer();

        // Assert
        result.Message.ShouldBe("Issuer is valid");
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateIssuer_InvalidIssuer_WhenValue_IsNotUri()
    {
        // Arrange
        const string tokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJpc3MiOiJjb25zdW1lcnN1cHBsaWVyIn0.";
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var testValidator = new TestValidator(token);

        // Act
        var result = testValidator.ValidateIssuer();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Issuer must contain the URL of auth server token endpoint");
    }

    [Fact]
    public void ValidateIssuer_InvalidIssuer_WhenValue_IsNullOrEmpty()
    {
        // Arrange
        const string tokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJpc3MiOiIifQ.";
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var testValidator = new TestValidator(token);

        // Act
        var result = testValidator.ValidateIssuer();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Issuer value cannot be null or empty");
    }

    #endregion

    #region Validate Subject

    [Fact]
    public void ValidateSubject_Should_Return_False_When_Subject_Is_Missing()
    {
        // Arrange
        var token = new JwtSecurityToken(claims: new List<Claim>()); // no "sub" claim
        var validator = new TestValidator(token);

        // Act
        var result = validator.ValidateSubject();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Subject cannot be null or empty");
    }

    [Fact]
    public void ValidateSubject_Should_Return_False_and_Reason_When_Subject_Does_Not_Match_Id()
    {
        var practitioner = new RequestingPractitioner
        {
            Id = "1",
            Identifier = new[]
            {
                new Identifier { System = "local", Value = "1" }
            },
            Name = new Name
            {
                Family = new[] { "Smith" },
                Given = new[] { "John" },
                Prefix = new[] { "Dr" }
            }
        };

        const string subject = "2";

        var json = JsonSerializer.Serialize(practitioner);
        var claims = new List<Claim>
        {
            new("sub", subject),
            new("requesting_practitioner", json)
        };

        var token = new JwtSecurityToken(claims: claims);
        var validator = new TestValidator(token);

        // Act
        var result = validator.ValidateSubject();

        // Arrange
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Subject and requesting_practitioner.Id mismatch");
    }

    [Fact]
    public void ValidateSubject_Should_Return_True_When_Subject_Is_Valid()
    {
        // Arrange
        var practitioner = new RequestingPractitioner
        {
            Id = "1",
            Identifier =
            [
                new Identifier { System = "local", Value = "1" }
            ],
            Name = new Name
            {
                Family = ["Smith"],
                Given = ["John"],
                Prefix = ["Dr"]
            }
        };

        var json = JsonSerializer.Serialize(practitioner);
        var claims = new List<Claim>
        {
            new("sub", "1"),
            new("requesting_practitioner", json)
        };

        var token = new JwtSecurityToken(claims: claims);
        var validator = new TestValidator(token);

        // Act
        var result = validator.ValidateSubject();

        // Assert
        result.Message.ShouldBe("Subject is valid.");
        result.IsValid.ShouldBeTrue();
    }

    #endregion
}