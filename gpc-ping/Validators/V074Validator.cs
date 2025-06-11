using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace gpc_ping.Validators;

public class V074Validator(JwtSecurityToken token) : BaseValidator(token)
{
    public override (bool IsValid, string Message) ValidateAudience()
    {
        var audience = token.Claims.FirstOrDefault(x => x.Type == "aud")?.Value;

        if (audience == null || string.IsNullOrWhiteSpace(audience))
        {
            return (false, "'aud' claim cannot be null or empty");
        }

        var value = token.Audiences.FirstOrDefault();

        return value == "https://authorize.fhir.nhs.net/token"
            ? (true, "'aud' claim is valid")
            : (false, "'aud' claim is not valid - see GP Connect specification");
    }

    public (bool IsValid, string[] Messages) ValidateRequestedRecord()
    {
        var claim = token.Claims.FirstOrDefault(x => x.Type == "requested_record");

        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return (false, ["'requested_record' claim cannot be null or empty"]);
        }

        try
        {
            List<string> messages = [];
            var deserialized = JsonSerializer.Deserialize<RequestedRecord>(claim.Value);


            if (deserialized == null)
            {
                return (false, ["'requested_record' claim cannot be null"]);
            }

            if (string.IsNullOrWhiteSpace(deserialized.ResourceType))
            {
                return (false, ["'resource_type' claim cannot be null or empty"]);
            }

            if (deserialized.Identifier == null || deserialized.Identifier.Length < 1)
            {
                return (false, ["'requested_record' claim is missing an identifier value"]);
            }


            for (var index = 0; index < deserialized.Identifier.Length; index++)
            {
                var identifier = deserialized.Identifier[index];
                if (string.IsNullOrWhiteSpace(identifier.System) || string.IsNullOrWhiteSpace(identifier.Value))
                {
                    messages.Add($"'requested_record' - identifier[{index}] claim is invalid");
                }
            }

            return messages.Count > 0 ? (false, messages.ToArray()) : (true, ["'requested_record' claim is valid"]);
        }
        catch (JsonException jsonException)
        {
            return (false, [jsonException.Message]);
        }
    }

    public override (bool IsValid, string Message) ValidateRequestingDevice()
    {
        var claim = token.Claims.FirstOrDefault(x => x.Type == "requesting_device");
        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return (false, "'requesting_device' claim cannot be null or empty");
        }

        V074RequestingDevice? requestingDevice;
        try
        {
            requestingDevice = JsonSerializer.Deserialize<V074RequestingDevice>(claim.Value);
        }
        catch (JsonException)
        {
            return (false, "Failed to parse 'requesting_device' claim");
        }

        if (requestingDevice == null)
        {
            return (false, "Invalid requesting device - see GP Connect specification");
        }

        var baseValidationResult = ValidationHelpers.ValidateRequestingDeviceCommon(requestingDevice);

        if (!baseValidationResult.IsValid)
            return baseValidationResult;

        return string.IsNullOrWhiteSpace(requestingDevice.Id)
            ? (false, "Invalid requesting device - missing Id")
            : (true, "The requesting device is valid.");
    }

    public override (bool, string) ValidateRequestingOrganization()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string[] Messages) ValidateRequestingPractitioner()
    {
        var (isValid, messages, requestingPractitioner) =
            ValidationHelpers.DeserializeAndValidateCommonRequestingPractitionerProperties(
                token.Claims.FirstOrDefault(x => x.Type == "requesting_practitioner"));

        if (!isValid)
        {
            return (false, messages);
        }

        const int requiredIdentifierLength = 3;
        var (isIdentifierValid, identifierMessages) =
            ValidationHelpers.ValidateRequestingPractitionerIdentifier(requestingPractitioner,
                requiredIdentifierLength);

        if (!isIdentifierValid)
            return (false, identifierMessages);


        // Check Practitioner Role
        if (requestingPractitioner.PractitionerRole.Length < 1)
            return (false, ["'requesting_practitioner': practitioner_role claim is invalid"]);

        if (requestingPractitioner.PractitionerRole.First().Role.Coding.Length < 1)
        {
            return (false, ["practitioner role: coding is missing or empty."]);
        }

        var coding = requestingPractitioner.PractitionerRole.First().Role.Coding.First();
        if (string.IsNullOrEmpty(coding.System) || string.IsNullOrEmpty(coding.Value))
        {
            return (false, ["practitioner role: coding is invalid"]);
        }

        return (true, ["'requesting_practitioner claim is valid"]);
    }
}