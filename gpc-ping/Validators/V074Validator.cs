using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace gpc_ping.Validators;

public class V074Validator : BaseValidator
{
    public V074Validator(JwtSecurityToken token, IValidationCommonValidation validationHelper) : base(token,
        validationHelper)
    {
    }

    public override (bool IsValid, string Message) ValidateAudience()
    {
        var audience = Token.Claims.FirstOrDefault(x => x.Type == "aud")?.Value;

        if (audience == null || string.IsNullOrWhiteSpace(audience))
        {
            return (false, "'aud' claim cannot be null or empty");
        }

        var value = Token.Audiences.FirstOrDefault();

        return value == "https://authorize.fhir.nhs.net/token"
            ? (true, "'aud' claim is valid")
            : (false, "'aud' claim is not valid - see GP Connect specification");
    }

    public (bool IsValid, string[] Messages) ValidateRequestedRecord()
    {
        var claim = Token.Claims.FirstOrDefault(x => x.Type == "requested_record");

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
        var claim = Token.Claims.FirstOrDefault(x => x.Type == "requesting_device");
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

        var baseValidationResult = _validationHelper.ValidateRequestingDeviceCommon(requestingDevice);

        if (!baseValidationResult.IsValid)
            return baseValidationResult;

        return string.IsNullOrWhiteSpace(requestingDevice.Id)
            ? (false, "Invalid requesting device - missing Id")
            : (true, "The requesting device is valid.");
    }

    public override (bool IsValid, string[] Messages) ValidateRequestingOrganization()
    {
        var (baseIsValid, baseValidationMessages, deserializedClaim) =
            _validationHelper.ValidateRequestingOrganizationCommon<V074RequestingOrganization>(Token);

        if (!baseIsValid || deserializedClaim == null)
        {
            return (false, baseValidationMessages);
        }

        if (string.IsNullOrEmpty(deserializedClaim.Id))
        {
            return (false, ["Invalid 'requesting_organization' - missing Id"]);
        }

        if (string.IsNullOrEmpty(deserializedClaim.Name))
        {
            return (false, ["Invalid 'requesting_organization' - missing Name"]);
        }

        return (true, ["'requesting_organization' is valid"]);
    }

    public override (bool IsValid, string[] Messages) ValidateRequestingPractitioner()
    {
        var (isValid, messages, requestingPractitioner) =
            _validationHelper.DeserializeAndValidateCommonRequestingPractitionerProperties<V074RequestingPractitioner>(
                Token.Claims.FirstOrDefault(x => x.Type == "requesting_practitioner"));

        if (!isValid || requestingPractitioner == null)
        {
            return (false, messages);
        }

        const int requiredIdentifierLength = 2;
        var (isIdentifierValid, identifierMessages) =
            _validationHelper.ValidateRequestingPractitionerIdentifier(requestingPractitioner,
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
        if (string.IsNullOrEmpty(coding.System) || string.IsNullOrEmpty(coding.Code))
        {
            return (false, ["practitioner role: coding is invalid"]);
        }

        return (true, ["'requesting_practitioner claim is valid"]);
    }
}