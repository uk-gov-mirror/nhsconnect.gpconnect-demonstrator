using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using gpc_ping;
using gpc_ping.Validators;
using Shouldly;
using gpc.Helpers;
using NSubstitute;


namespace gpc;

public class V074ValidatorTests
{
    # region Audience

    [Fact]
    public void ValidateAudience_ValidAudience()
    {
        // Arrange
        const string tokenString =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJodHRwczovL2F1dGhvcml6ZS5maGlyLm5ocy5uZXQvdG9rZW4ifQ.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var validator = new V074Validator(token, new ValidationHelper());

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("'aud' claim is valid");
    }

    [Theory]
    [InlineData("eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJ0b2tlbiJ9.")] // no uri
    [InlineData(
        "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiJodHRwczovL3RoaXJkcGFydHlwcm92aWRlci5uZXQifQ.")] // wrong uri
    public void ValidateAudience_InvalidAudience(string tokenString)
    {
        // Arrange
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var validator = new V074Validator(token, new ValidationHelper());

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'aud' claim is not valid - see GP Connect specification");
    }


    [Theory]
    [InlineData("")] // empty aud
    [InlineData(" ")] // whitespace
    public void ValidateAudience_InvalidAudience_EmptyOrWhitespaceAudience(string? tokenString)
    {
        // Arrange
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "aud", tokenString }
        });

        // Act
        var result = validator.ValidateAudience();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'aud' claim cannot be null or empty");
    }

    #endregion

    #region Reason for request

    [Fact]
    public void ValidateReasonForRequest_ReturnsTrue_WhenValid()
    {
        // Arrange
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>()
        {
            { "reason_for_request", "directcare" }
        });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("'reason_for_request' is valid.");
    }

    [Fact]
    public void ValidateReasonForRequest_ReturnsFalse_WhenMissingClaim()
    {
        // Arrange
        var validator =
            TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>());

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
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>()
        {
            { "reason_for_request", "patient_data" }
        });

        // Act
        var result = validator.ValidateReasonForRequest();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe($"Invalid 'reason_for_request': 'patient_data'");
    }

    #endregion

    #region Requesting Device

    [Fact]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_ClaimIsMissing()
    {
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>());

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'requesting_device' claim cannot be null or empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_ClaimValueIsEmptyOrWhitespace(string claimValue)
    {
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", claimValue } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("'requesting_device' claim cannot be null or empty");
    }

    [Fact]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_ClaimValueIsInvalidJson()
    {
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", "{ invalid json" } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Failed to parse 'requesting_device' claim");
    }


    [Fact]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_DeserializedDeviceIsNull()
    {
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", "null" } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Fact]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_IdentifierIsEmpty()
    {
        var device = new V074RequestingDevice()
        {
            Identifier = Array.Empty<Identifier>(),
            Model = "ModelX",
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_FirstIdentifierSystemIsNullOrWhitespace(string system)
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Identifier = new[]
            {
                new Identifier { System = system, Value = "some-value" }
            },
            Model = "ModelX",
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Fact]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_FirstIdentifierSystemIsInvalidUrl()
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Identifier = new[]
            {
                new Identifier { System = "not-a-valid-url", Value = "some-value" }
            },
            Model = "ModelX",
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_FirstIdentifierValueIsNullOrWhitespace(string value)
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Identifier = new[]
            {
                new Identifier { System = "https://valid.url", Value = value }
            },
            Model = "ModelX",
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_ModelIsNullOrWhitespace(string model)
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Identifier = new[]
            {
                new Identifier { System = "https://valid.url", Value = "value" }
            },
            Model = model,
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequestingDevice_Should_ReturnFalse_When_VersionIsNullOrWhitespace(string version)
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Identifier = new[]
            {
                new Identifier { System = "https://valid.url", Value = "value" }
            },
            Model = "ModelX",
            Version = version
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Fact]
    public void ValidateRequestingDevice_Should_ReturnTrue_When_RequestingDeviceIsValid()
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Id = "deviceId",
            Identifier = new[]
            {
                new Identifier { System = "https://valid.url", Value = "device-123" }
            },
            Model = "ModelX",
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("The requesting device is valid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_RequestingDevice_Should_ReturnFalse_WhenRequestingDeviceId_IsMissing(
        string requestingDeviceId)
    {
        var device = new V074RequestingDevice()
        {
            ResourceType = "ResourceType",
            Id = requestingDeviceId,
            Identifier = new[]
            {
                new Identifier { System = "https://valid.url", Value = "device-123" }
            },
            Model = "ModelX",
            Version = "1.0"
        };
        var json = JsonSerializer.Serialize(device);

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(
            new Dictionary<string, string> { { "requesting_device", json } });

        var result = validator.ValidateRequestingDevice();

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - missing Id");
    }

    #endregion

    #region Requested Record

    [Fact]
    public void Validate_RequestedRecord_Should_ReturnTrue_When_Valid()
    {
        var requestingRecord = new RequestedRecord()
        {
            ResourceType = "Patient", Identifier =
            [
                new Identifier { System = "https://valid.url", Value = "device-123" }
            ]
        };
        var json = JsonSerializer.Serialize(requestingRecord);

        // Arrange
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
            { { "requested_record", json } });

        // Act

        var result = validator.ValidateRequestedRecord();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Messages.Length.ShouldBe(1);
        result.Messages[0].ShouldBe("'requested_record' claim is valid");
    }

    [Fact]
    public void ValidateRequestedRecord_ReturnsInvalid_WhenClaimIsMissing()
    {
        // Arrange
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>());

        // Act
        var result = validator.ValidateRequestedRecord();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'requested_record' claim cannot be null or empty");
    }

    [Fact]
    public void ValidateRequestedRecord_ReturnsInvalid_WhenClaimValueIsEmpty()
    {
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requested_record", string.Empty }
        });

        var result = validator.ValidateRequestedRecord();

        Assert.False(result.IsValid);
        Assert.Contains("'requested_record' claim cannot be null or empty", result.Messages);
    }

    [Fact]
    public void ValidateRequestedRecord_ReturnsInvalid_WhenResourceTypeIsMissing()
    {
        var json = JsonSerializer.Serialize(new
        {
            Identifier = new[] { new { System = "http://system", Value = "123" } }
        });

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requested_record", json }
        });

        var result = validator.ValidateRequestedRecord();

        Assert.False(result.IsValid);
        Assert.Contains("'resource_type' claim cannot be null or empty", result.Messages);
    }

    [Fact]
    public void ValidateRequestedRecord_ReturnsInvalid_WhenIdentifierIsNull()
    {
        var json = JsonSerializer.Serialize(new { ResourceType = "Patient", Identifier = (object)null! });
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requested_record", json }
        });

        var result = validator.ValidateRequestedRecord();

        Assert.False(result.IsValid);
        Assert.Contains("'requested_record' claim is missing an identifier value", result.Messages);
    }

    [Fact]
    public void ValidateRequestedRecord_ReturnsInvalid_WhenIdentifierIsEmpty()
    {
        var json = JsonSerializer.Serialize(new { ResourceType = "Patient", Identifier = new object[] { } });
        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requested_record", json }
        });
        var result = validator.ValidateRequestedRecord();

        Assert.False(result.IsValid);
        Assert.Contains("'requested_record' claim is missing an identifier value", result.Messages);
    }

    [Fact]
    public void ValidateRequestedRecord_ReturnsInvalid_WhenIdentifierHasEmptySystemOrValue()
    {
        var json = JsonSerializer.Serialize(new
        {
            ResourceType = "Patient",
            Identifier = new[] { new { System = "", Value = "" } }
        });

        var validator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requested_record", json }
        });

        var result = validator.ValidateRequestedRecord();

        Assert.False(result.IsValid);
        Assert.Contains("'requested_record' - identifier[0] claim is invalid", result.Messages);
    }

    #endregion

    #region Requesting Organization

    [Fact]
    public void ValidateRequestingOrganization_ShouldCall_ValidationHelpers_ValidateRequestingOrganizationCommon()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJJZCI6IjAxMDEwIiwiTmFtZSI6InRlc3QtbmFtZSIsIlJlc291cmNlVHlwZSI6Ik9yZ2FuaXphdGlvbiIsIklkZW50aWZpZXIiOlt7IlN5c3RlbSI6Imh0dHBzOi8vdmFsaWQudXJsIiwiVmFsdWUiOiIwMTkxMjMifV19.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        var mockHelper = Substitute.For<IValidationCommonValidation>();
        mockHelper.ValidateRequestingOrganizationCommon<V074RequestingOrganization>(token).Returns((true, [], null));

        var testValidator = new V074Validator(token, mockHelper);

        // Act
        testValidator.ValidateRequestingOrganization();

        // Assert
        mockHelper
            .Received(1)
            .ValidateRequestingOrganizationCommon<V074RequestingOrganization>(token);
    }

    [Fact]
    public void ValidateRequestingOrganization_ReturnsTrue_WhenValid()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJJZCI6IjAxMDEwIiwiTmFtZSI6InRlc3QtbmFtZSIsIlJlc291cmNlVHlwZSI6Ik9yZ2FuaXphdGlvbiIsIklkZW50aWZpZXIiOlt7IlN5c3RlbSI6Imh0dHBzOi8vdmFsaWQudXJsIiwiVmFsdWUiOiIwMTkxMjMifV19.";

        var deserializedClaim = new V074RequestingOrganization()
        {
            Id = "01010",
            Name = "testname",
            ResourceType = "Organization",
            Identifier =
            [
                new Identifier()
                {
                    System = "https://valid.url",
                    Value = "019123"
                }
            ]
        };

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        var mockHelper = Substitute.For<IValidationCommonValidation>();
        mockHelper.ValidateRequestingOrganizationCommon<V074RequestingOrganization>(token)
            .Returns((true, ["Returning message from helper"], deserializedClaim));

        var testValidator = new V074Validator(token, mockHelper);

        // Act
        var result = testValidator.ValidateRequestingOrganization();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Messages.Length.ShouldBe(1);
        result.Messages[0].ShouldBe("'requesting_organization' is valid");
    }

    [Fact]
    public void ValidateRequestingOrganization_ReturnsFalse_When_IdIsEmpty()
    {
        // Arrange
        var testOrg = new V074RequestingOrganization()
        {
            Id = string.Empty,
            Name = "testname",
            ResourceType = "Organization",
            Identifier =
            [
                new Identifier() { System = "https://valid.url", Value = "019123" }
            ]
        };
        var json = JsonSerializer.Serialize(testOrg);

        var testValidator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requesting_organization", json }
        });

        // Act
        var result = testValidator.ValidateRequestingOrganization();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Messages.Length.ShouldBe(1);
        result.Messages[0].ShouldBe("Invalid 'requesting_organization' - missing Id");
    }

    [Fact]
    public void ValidateRequestingOrganization_ReturnsFalse_When_NameIsEmpty()
    {
        // Arrange
        var testOrg = new V074RequestingOrganization()
        {
            Id = "010101",
            Name = string.Empty,
            ResourceType = "Organization",
            Identifier =
            [
                new Identifier() { System = "https://valid.url", Value = "019123" }
            ]
        };
        var json = JsonSerializer.Serialize(testOrg);

        var testValidator = TestHelpers.CreateValidatorWithToken<V074Validator>(new Dictionary<string, string>
        {
            { "requesting_organization", json }
        });

        // Act
        var result = testValidator.ValidateRequestingOrganization();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Messages.Length.ShouldBe(1);
        result.Messages[0].ShouldBe("Invalid 'requesting_organization' - missing Name");
    }

    #endregion

    #region ValidateRequestingPractitioner

    [Fact]
    public void ValidateRequestingPractitioner_InvalidDeserialization_ReturnsFalse()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJJZCI6IjAxMDEwIiwiTmFtZSI6InRlc3QtbmFtZSIsIlJlc291cmNlVHlwZSI6Ik9yZ2FuaXphdGlvbiIsIklkZW50aWZpZXIiOlt7IlN5c3RlbSI6Imh0dHBzOi8vdmFsaWQudXJsIiwiVmFsdWUiOiIwMTkxMjMifV19.";

        var mockHelper = Substitute.For<IValidationCommonValidation>();
        mockHelper
            .DeserializeAndValidateCommonRequestingPractitionerProperties<V074RequestingPractitioner>(Arg.Any<Claim>())
            .Returns((false, new[] { "invalid claim" }, null));


        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var testValidator = new V074Validator(token, mockHelper);


        // Act
        var result = testValidator.ValidateRequestingPractitioner();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("invalid claim");
    }

    [Fact]
    public void ValidateRequestingPractitioner_InvalidIdentifier_ReturnsFalse()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJJZCI6IjAxMDEwIiwiTmFtZSI6InRlc3QtbmFtZSIsIlJlc291cmNlVHlwZSI6Ik9yZ2FuaXphdGlvbiIsIklkZW50aWZpZXIiOlt7IlN5c3RlbSI6Imh0dHBzOi8vdmFsaWQudXJsIiwiVmFsdWUiOiIwMTkxMjMifV19.";

        var dummyPractitioner = new V074RequestingPractitioner();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var mockHelper = Substitute.For<IValidationCommonValidation>();
        var testValidator = new V074Validator(token, mockHelper);

        mockHelper
            .DeserializeAndValidateCommonRequestingPractitionerProperties<V074RequestingPractitioner>(Arg.Any<Claim>())
            .Returns((true, [], dummyPractitioner));

        mockHelper
            .ValidateRequestingPractitionerIdentifier(dummyPractitioner, 2)
            .Returns((false, new[] { "bad identifier" }));

        // Act
        var result = testValidator.ValidateRequestingPractitioner();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("bad identifier", result.Messages);
    }

    [Fact]
    public void ValidateRequestingPractitioner_ValidFlow_ReturnsTrue()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJyZXF1ZXN0aW5nX3ByYWN0aXRpb25lciI6eyJyZXNvdXJjZVR5cGUiOiJQcmFjdGl0aW9uZXIiLCJpZCI6ImY3NzM3YmY1LWNmZTctNDkxYy1hZjNhLTZmNzE3N2Q1YWVlMSIsImlkZW50aWZpZXIiOlt7InN5c3RlbSI6Imh0dHA6Ly9maGlyLm5ocy5uZXQvc2RzLXVzZXItaWQiLCJ2YWx1ZSI6IjExMTIyMjMzMzQ0NCJ9LHsic3lzdGVtIjoiaHR0cDovL2NvbnN1bWVyc3VwcGxpZXIuY29tL0lkL3VzZXItZ3VpZCIsInZhbHVlIjoiNTRiOWQ5ODctYzJmMS00ZmRkLWE0NDktZTY3Y2RmNDFkZDJiIn1dLCJuYW1lIjp7ImZhbWlseSI6WyJKb25lcyJdLCJnaXZlbiI6WyJDbGFpcmUiXSwicHJlZml4IjpbIkRyIl19LCJwcmFjdGl0aW9uZXJSb2xlIjpbeyJyb2xlIjp7ImNvZGluZyI6W3sic3lzdGVtIjoiaHR0cDovL2ZoaXIubmhzLm5ldC9WYWx1ZVNldC9zZHMtam9iLXJvbGUtbmFtZS0xIiwiY29kZSI6IlI4MDAwIn1dfX1dfX0.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var mockHelper = Substitute.For<IValidationCommonValidation>();

        var dummyPractitioner = new V074RequestingPractitioner
        {
            ResourceType = "Practitioner",
            Id = "f7737bf5-cfe7-491c-af3a-6f7177d5aee1",
            Identifier = new[]
            {
                new Identifier
                {
                    System = "http://fhir.nhs.net/sds-user-id",
                    Value = "111222333444"
                },
                new Identifier
                {
                    System = "http://consumersupplier.com/Id/user-guid",
                    Value = "54b9d987-c2f1-4fdd-a449-e67cdf41dd2b"
                }
            },
            Name = new Name
            {
                Family = new[]
                {
                    "Jones"
                },
                Given = new[] { "Claire" },
                Prefix = new[] { "Dr" }
            },
            PractitionerRole = new[]
            {
                new PractitionerRole
                {
                    Role = new Role
                    {
                        Coding = new[]
                        {
                            new Coding
                            {
                                System = "http://fhir.nhs.net/ValueSet/sds-job-role-name-1",
                                Code = "R8000"
                            }
                        }
                    }
                }
            }
        };
        var testValidator = new V074Validator(token, mockHelper);

        mockHelper
            .DeserializeAndValidateCommonRequestingPractitionerProperties<V074RequestingPractitioner>(Arg.Any<Claim>())
            .Returns((true, [], dummyPractitioner));

        mockHelper
            .ValidateRequestingPractitionerIdentifier(dummyPractitioner, 2)
            .Returns((true, []));

        // Act
        var result = testValidator.ValidateRequestingPractitioner();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Messages.ShouldContain("'requesting_practitioner claim is valid");
    }

    [Fact]
    public void ValidateRequestingPractitioner_CallsDeserializeAndValidateRequestingPractitionerProperties()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJyZXF1ZXN0aW5nX3ByYWN0aXRpb25lciI6eyJpZCI6ImY3NzM3YmY1LWNmZTctNDkxYy1hZjNhLTZmNzE3N2Q1YWVlMSIsImlkZW50aWZpZXIiOlt7InN5c3RlbSI6Imh0dHA6Ly9maGlyLm5ocy5uZXQvc2RzLXVzZXItaWQiLCJ2YWx1ZSI6IjExMTIyMjMzMzQ0NCJ9LHsic3lzdGVtIjoiaHR0cDovL2NvbnN1bWVyc3VwcGxpZXIuY29tL0lkL3VzZXItZ3VpZCIsInZhbHVlIjoiNTRiOWQ5ODctYzJmMS00ZmRkLWE0NDktZTY3Y2RmNDFkZDJiIn1dLCJuYW1lIjp7ImZhbWlseSI6WyJKb25lcyJdLCJnaXZlbiI6WyJDbGFpcmUiXSwicHJlZml4IjpbIkRyIl19fX0.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);
        var mockHelper = Substitute.For<IValidationCommonValidation>();

        var dummyPractitioner = new V074RequestingPractitioner()
        {
            Id = "10101",
            Identifier =
            [
                new Identifier()
                {
                    System = "https://fhir.nhs.uk/Id/sds-user-id",
                    Value = "111222333444"
                },
                new Identifier()
                {
                    System = "https://fhir.nhs.uk/Id/sds-role-profile-id",
                    Value = "444555666777"
                },
                new Identifier()
                {
                    System = "https://consumersupplier.com/Id/user-guid",
                    Value = "98ed4f78-814d-4266-8d5b-cde742f3093c"
                }
            ],
            Name =
                new Name()
                {
                    Family = ["Stark"], Given = ["Tony"], Prefix = ["Mr"]
                }
        };

        var testValidator = new V074Validator(token, mockHelper);

        mockHelper
            .DeserializeAndValidateCommonRequestingPractitionerProperties<V074RequestingPractitioner>(Arg.Any<Claim>())
            .Returns((true, [], dummyPractitioner));

        mockHelper
            .ValidateRequestingPractitionerIdentifier(dummyPractitioner, 2)
            .Returns((true, []));

        // Act
        testValidator.ValidateRequestingPractitioner();

        // Assert
        mockHelper.Received(1).ValidateRequestingPractitionerIdentifier(dummyPractitioner, 2);
    }

    #endregion
}