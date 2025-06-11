using System.Security.Claims;
using System.Text.Json;
using gpc_ping.Validators;
using Shouldly;

namespace gpc_ping.Tests;

public class ValidationHelpersTests
{
    #region ValidateRequestingDeviceCommonProps

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenIdentifierIsNull()
    {
        var device = new RequestingDevice
        {
            Identifier = null
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenIdentifierIsEmpty()
    {
        var device = new RequestingDevice
        {
            Identifier = Array.Empty<Identifier>()
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldBe("Invalid requesting device - see GP Connect specification");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenFirstIdentifierSystemIsMissing()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = null, Value = "val" } },
            Model = "model",
            Version = "v1"
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldContain("Invalid requesting device");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenFirstIdentifierValueIsMissing()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "http://valid.url", Value = null } },
            Model = "model",
            Version = "v1"
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldContain("Invalid requesting device");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenModelIsMissing()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "http://valid.url", Value = "123" } },
            Model = null,
            Version = "v1"
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldContain("Invalid requesting device");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenVersionIsMissing()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "http://valid.url", Value = "123" } },
            Model = "model",
            Version = null
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldContain("Invalid requesting device");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalse_WhenSystemIsInvalidUrl()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "bad_url", Value = "123" } },
            Model = "model",
            Version = "v1"
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldContain("Invalid requesting device");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsTrue_WhenAllFieldsValid_AndResourceTypePresent()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "http://valid.url", Value = "123" } },
            Model = "model",
            Version = "v1",
            ResourceType = "Device"
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldBe("The requesting device is valid.");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsTrueWithWarning_WhenResourceTypeMissing()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "http://valid.url", Value = "123" } },
            Model = "model",
            Version = "v1",
            ResourceType = null
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeTrue();
        result.Message.ShouldContain("The requesting device is valid.");
        result.Message.ShouldContain("warning: resource_type is missing or empty.");
    }

    [Fact]
    public void ValidateRequestingDeviceCommon_ReturnsFalseWithWarning_WhenInvalidFields_AndResourceTypeMissing()
    {
        var device = new RequestingDevice
        {
            Identifier = new[] { new Identifier { System = "invalid_url", Value = "123" } },
            Model = "model",
            Version = "v1",
            ResourceType = ""
        };

        var result = ValidationHelpers.ValidateRequestingDeviceCommon(device);

        result.IsValid.ShouldBeFalse();
        result.Message.ShouldContain("Invalid requesting device");
        result.Message.ShouldContain("warning: resource_type is missing or empty.");
    }

    #endregion

    #region ValidateRequestingPractitionerProps

    [Fact]
    public void ValidateRequestingPractitionerIdentifier_ReturnsFalse_WhenIdentifierIsEmpty()
    {
        var practitioner = new RequestingPractitioner
        {
            Identifier = Array.Empty<Identifier>()
        };

        var result = ValidationHelpers.ValidateRequestingPractitionerIdentifier(practitioner, 1);

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'requesting_practitioner.identifier' is missing or empty.");
    }

    [Fact]
    public void ValidateRequestingPractitionerIdentifier_ReturnsFalse_WhenIdentifierLengthDoesNotMatchRequired()
    {
        var practitioner = new RequestingPractitioner
        {
            Identifier = new[] { new Identifier { System = "sys", Value = "val" } }
        };

        var result = ValidationHelpers.ValidateRequestingPractitionerIdentifier(practitioner, 2);

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'requesting practitioner' claim does not match the required length");
    }

    [Fact]
    public void ValidateRequestingPractitionerIdentifier_ReturnsFalse_WhenSystemIsMissing()
    {
        var practitioner = new RequestingPractitioner
        {
            Identifier = new[] { new Identifier { System = null, Value = "val" } }
        };

        var result = ValidationHelpers.ValidateRequestingPractitionerIdentifier(practitioner, 1);

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("identifier:[0] system' is missing or empty.");
    }

    [Fact]
    public void ValidateRequestingPractitionerIdentifier_ReturnsFalse_WhenValueIsMissing()
    {
        var practitioner = new RequestingPractitioner
        {
            Identifier = new[] { new Identifier { System = "sys", Value = null } }
        };

        var result = ValidationHelpers.ValidateRequestingPractitionerIdentifier(practitioner, 1);

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("identifier:[0] value' is missing or empty.");
    }

    [Fact]
    public void ValidateRequestingPractitionerIdentifier_ReturnsFalse_WhenMultipleIdentifiersHaveErrors()
    {
        var practitioner = new RequestingPractitioner
        {
            Identifier = new[]
            {
                new Identifier { System = null, Value = null },
                new Identifier { System = "", Value = "" }
            }
        };

        var result = ValidationHelpers.ValidateRequestingPractitionerIdentifier(practitioner, 2);

        result.IsValid.ShouldBeFalse();
        result.Messages.Length.ShouldBe(4);
        result.Messages.ShouldContain("identifier:[0] system' is missing or empty.");
        result.Messages.ShouldContain("identifier:[0] value' is missing or empty.");
        result.Messages.ShouldContain("identifier:[1] system' is missing or empty.");
        result.Messages.ShouldContain("identifier:[1] value' is missing or empty.");
    }

    [Fact]
    public void ValidateRequestingPractitionerIdentifier_ReturnsTrue_WhenAllIdentifiersAreValid()
    {
        var practitioner = new RequestingPractitioner
        {
            Identifier = new[]
            {
                new Identifier { System = "http://sys1", Value = "val1" },
                new Identifier { System = "http://sys2", Value = "val2" }
            }
        };

        var result = ValidationHelpers.ValidateRequestingPractitionerIdentifier(practitioner, 2);

        result.IsValid.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
    }

    #endregion

    #region ValidateRequestingPractitioner

    [Fact]
    public void ReturnsFalse_WhenClaimIsNull()
    {
        var result = ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(null);

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("Missing 'requesting_practitioner' claim");
        result.requestingPractitioner.ShouldBeNull();
    }

    [Fact]
    public void ReturnsFalse_WhenClaimValueIsNullOrEmpty()
    {
        var result =
            ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(
                new Claim("requesting_practitioner", ""));

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'requesting_practitioner' value cannot be null or empty");
        result.requestingPractitioner.ShouldBeNull();
    }

    [Fact]
    public void ReturnsFalse_WhenJsonIsInvalid()
    {
        var result = ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(
            new Claim("requesting_practitioner", "{this is not valid JSON string}"));

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("Invalid JSON in 'requesting_practitioner' claim.");
        result.requestingPractitioner.ShouldBeNull();
    }

    [Fact]
    public void ReturnsFalse_WhenPractitionerIsNullOrIdIsMissing()
    {
        var json = JsonSerializer.Serialize(new RequestingPractitioner { Id = null });
        var result = ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(
            new Claim("requesting_practitioner", json));

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("'requesting_practitioner.id' is missing or empty.");
        result.requestingPractitioner.ShouldNotBeNull();
    }

    [Fact]
    public void ReturnsFalse_WhenNamePropertiesAreMissing()
    {
        var practitioner = new RequestingPractitioner
        {
            Id = "123",
            Name = new Name
            {
                Family = Array.Empty<string>(),
                Given = Array.Empty<string>(),
                Prefix = Array.Empty<string>()
            }
        };

        var json = JsonSerializer.Serialize(practitioner);
        var result = ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(
            new Claim("requesting_practitioner", json));

        result.IsValid.ShouldBeFalse();
        result.Messages.ShouldContain("name: family name is missing or empty.");
        result.Messages.ShouldContain("name: given name is missing or empty.");
        result.Messages.ShouldContain("name: prefix is missing or empty.");
    }

    [Fact]
    public void ReturnsTrue_WhenAllFieldsAreValid()
    {
        var practitioner = new RequestingPractitioner
        {
            Id = "123",
            Name = new Name
            {
                Family = new[] { "Smith" },
                Given = new[] { "John" },
                Prefix = new[] { "Dr" }
            }
        };

        var json = JsonSerializer.Serialize(practitioner);
        var result = ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(
            new Claim("requesting_practitioner", json));

        result.IsValid.ShouldBeTrue();
        result.Messages.ShouldContain("'requesting_practitioner': name, id valid ");
        result.requestingPractitioner.ShouldNotBeNull();
        result.requestingPractitioner!.Id.ShouldBe("123");
    }

    #endregion
}