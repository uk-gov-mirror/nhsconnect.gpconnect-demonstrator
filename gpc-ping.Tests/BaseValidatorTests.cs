using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Shouldly;
using gpc_ping;
using gpc.Helpers;
using static gpc.Helpers.TestHelpers;

public class BaseValidatorTests
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

    [Fact]
    public void ValidateSubject_ShouldReturnFalse_When_RequestingPractitionerIsMissing()
    {
        // Arrange
        var validator = TestHelpers.CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>
        {
            { "sub", "2119" }
        });

        // Arrange
        var result = validator.ValidateSubject();

        // Act
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Missing 'requesting_practitioner' claim.");
    }

    #endregion

    #region ValidateLifetime

    [Fact]
    public void Should_ReturnFalse_When_IatClaimMissing()
    {
        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "exp", "1717776000" }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("Missing 'iat' claim.");
    }

    [Fact]
    public void Should_ReturnFalse_When_ExpClaimMissing()
    {
        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", "1717775700" }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("Missing 'exp' claim.");
    }

    [Fact]
    public void Should_ReturnFalse_When_IatNotValidUnixTime()
    {
        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", "not-a-number" },
            { "exp", "1717776000" }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'iat' claim is not a valid Unix time.");
    }

    [Fact]
    public void Should_ReturnFalse_When_ExpNotValidUnixTime()
    {
        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", "1717775700" },
            { "exp", "not-a-number" }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'exp' claim is not a valid Unix time.");
    }

    [Fact]
    public void Should_ReturnTrue_When_LifetimeIsExactlyFiveMinutes()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var fiveMinutesLater = now + 300;

        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", now.ToString() },
            { "exp", fiveMinutesLater.ToString() }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeTrue();
        result.Messages.ShouldContain("Lifetime is valid");
    }

    [Fact]
    public void Should_ReturnFalse_When_LifetimeIsTooShort()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var fourMinutesLater = now + 240;

        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", now.ToString() },
            { "exp", fourMinutesLater.ToString() }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("Lifetime of claim is not valid");
    }

    [Fact]
    public void Should_ReturnFalse_When_LifetimeIsTooLong()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sixMinutesLater = now + 360;

        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", now.ToString() },
            { "exp", sixMinutesLater.ToString() }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("Lifetime of claim is not valid");
    }

    [Fact]
    public void Should_ReturnAllErrors_When_ClaimsAreMissingOrInvalid()
    {
        var validator = CreateValidatorWithToken<TestValidator>(new()
        {
            { "iat", "not-a-number" },
            { "exp", "" }
        });

        var result = validator.ValidateLifetime();

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'iat' claim is not a valid Unix time.");
        result.Messages.ShouldContain("Missing 'exp' claim.");
    }

    #endregion

    #region ValidateAudience

    [Fact]
    public void Should_ReturnTrueWhen_AudienceIsValid()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>
        {
            { "aud", "https://providersupplier.thirdparty.nhs.uk/GP0001/STU3/1" }
        });

        var result = validator.ValidateAudience();

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("Audience is valid");
    }

    [Fact]
    public void Should_ReturnFalseWhen_AudienceIsMissing()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>());

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Audience is not valid - must have value");
    }

    [Fact]
    public void Should_ReturnFalseWhen_AudienceIsInvalid()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>
        {
            { "aud", "invalid-audience" }
        });

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Audience is not valid - see GP Connect specification");
    }

    [Fact]
    public void Should_ReturnFalseWhen_AudienceIsNullOrEmpty()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>
        {
            { "aud", "" }
        });

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Audience claim not valid - must have value");
    }

    #endregion

    #region ValidateRequestedScope

    [Theory]
    [InlineData(new[] { "patient/*.read", "organization/*.read" }, "patient/*.read")]
    [InlineData(new[] { "patient/*.read", "organization/*.read" }, "organization/*.read")]
    public void ShouldReturnTrue_When_RequestedScopeIsAccepted(string[] acceptedScopes, string requestScopeClaim)
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>()
        {
            { "requested_scope", requestScopeClaim }
        });

        // Act
        var result = validator.ValidateRequestedScope(acceptedScopes);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("'requested_scope' claim is valid");
    }


    [Theory]
    [InlineData(new[] { "patient/*.read", "organization/*.read" }, "patient/.*read")]
    [InlineData(new[] { "patient/*.read", "organization/*.read" }, "random_scope")]
    public void ShouldReturnFalse_When_RequestedScopeIsNotAccepted(string[] acceptedScopes,
        string requestScopeClaim)
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(
            new Dictionary<string, string>()
            {
                { "requested_scope", requestScopeClaim }
            });

        // Act
        var result = validator.ValidateRequestedScope(acceptedScopes);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid 'requested_scope' claim - claim contains invalid value(s)");
    }

    [Fact]
    public void ShouldReturnFalse_When_RequestedScopeClaimIsMissing()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>());

        // Act
        var result = validator.ValidateRequestedScope(new[] { "patient/*.read", "organization/*.read" });

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Missing 'requested_scope' claim");
    }

    [Fact]
    public void ShouldReturnFalse_When_RequestedScopeClaimIsNullOrEmpty()
    {
        // Arrange
        var validator =
            TestHelpers.CreateValidatorWithToken<TestValidator>(
                new Dictionary<string, string> { { "requested_scope", "" } });

        // Act
        var result = validator.ValidateRequestedScope(["patient/*.read", "organization/*.read"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'requested_scope' claim cannot be null or empty");
    }

    [Theory]
    [InlineData("patient/*.read patient/*.write")]
    [InlineData("patient/*.read patient/*.write patient/*.delete")]
    public void ShouldReturnFalse_When_RequestedScopeClaimHasIncorrectNumberOfScopes(string requestScopeClaim)
    {
        // Arrange
        var validator = TestHelpers.CreateValidatorWithToken<TestValidator>(
            new Dictionary<string, string>()
            {
                { "requested_scope", requestScopeClaim }
            });

        // Act
        var result = validator.ValidateRequestedScope(["patient/*.read"]);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'requested_scope' claim must 1 value");
    }

    [Fact]
    public void ValidateRequestedScope_ThrowsArgumentException_WhenAcceptedClaimValuesIsNull()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>
        {
            { "requested_scope", "patient/*.read" }
        });

        // Act
        var exception = Should.Throw<ArgumentException>(() => validator.ValidateRequestedScope(null));

        // Assert
        exception.Message.ShouldContain("acceptedClaims must not be null or empty");
        exception.ParamName.ShouldBe("acceptedClaimValues");
    }

    [Fact]
    public void ValidateRequestedScope_ThrowsArgumentException_WhenAcceptedClaimValuesIsEmpty()
    {
        // Arrange
        var validator = CreateValidatorWithToken<TestValidator>(new Dictionary<string, string>
        {
            { "requested_scope", "patient/*.read" }
        });

        // Act
        var exception = Should.Throw<ArgumentException>(() => validator.ValidateRequestedScope([]));

        // Assert
        exception.Message.ShouldContain("acceptedClaims must not be null or empty");
        exception.ParamName.ShouldBe("acceptedClaimValues");
    }

    #endregion
}