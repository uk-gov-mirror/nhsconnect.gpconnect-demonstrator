using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace gpc_ping.Validators;

public class ValidationHelper : IValidationCommonValidation
{
    /// <summary>
    /// Deserializes RequestingPractitioner claim, and validates common properties across spec versions
    /// </summary>
    /// <param name="requestingPractitionerClaim"></param>
    /// <returns></returns>
    public (bool IsValid, string[] Messages, T? Practitioner)
        DeserializeAndValidateCommonRequestingPractitionerProperties<T>(Claim? requestingPractitionerClaim)
        where T : RequestingPractitioner
    {
        if (requestingPractitionerClaim == null)
        {
            return (false, ["Missing 'requesting_practitioner' claim"], null);
        }

        if (string.IsNullOrEmpty(requestingPractitionerClaim.Value))
        {
            return (false, ["'requesting_practitioner' value cannot be null or empty"], null);
        }

        T? parsedPractitioner;
        try
        {
            var deserializedObject = JsonService.DeserializeClaim<T>(requestingPractitionerClaim.Value);
            if (deserializedObject == null)
            {
                return (false, ["Invalid 'requesting_practitioner'"], null);
            }

            parsedPractitioner = deserializedObject;
        }
        catch (JsonException)
        {
            return (false, ["Invalid JSON in 'requesting_practitioner' claim."], null);
        }

        if (parsedPractitioner == null || string.IsNullOrEmpty(parsedPractitioner.Id))
        {
            return (false, ["'requesting_practitioner.id' is missing or empty."], parsedPractitioner);
        }

        // Shared validation on base properties
        var messages = new List<string>();

        if (parsedPractitioner.Name == null)
        {
            return (false, ["Name property is missing or empty."], parsedPractitioner);
        }

        if (parsedPractitioner.Name.Family.IsNullOrEmpty())
        {
            messages.Add("name: family name is missing or empty.");
        }

        if (parsedPractitioner.Name.Given.IsNullOrEmpty())
        {
            messages.Add("name: given name is missing or empty.");
        }

        if (parsedPractitioner.Name.Prefix.Length == 0)
        {
            messages.Add("name: prefix is missing or empty.");
        }

        return messages.Count == 0
            ? (true, ["'requesting_practitioner': name, id valid "], parsedPractitioner)
            : (false, messages.ToArray(), parsedPractitioner);
    }

    public (bool IsValid, string[] Messages) ValidateRequestingPractitionerIdentifier(
        RequestingPractitioner requestingPractitioner, int requiredLength)
    {
        if (requestingPractitioner.Identifier.Length == 0)
        {
            return (false, ["'requesting_practitioner.identifier' is missing or empty."]);
        }

        if (requestingPractitioner.Identifier.Length != requiredLength)
        {
            return (false, ["'requesting practitioner' claim does not match the required length"]);
        }

        var messages = new List<string>();
        for (var index = 0; index < requestingPractitioner.Identifier.Length; index++)
        {
            var identifier = requestingPractitioner.Identifier[index];
            if (string.IsNullOrEmpty(identifier.System))
            {
                messages.Add($"{ClaimNames.RequestingPractitioner}:identifier:[{index}] system' is missing or empty.");
            }

            if (string.IsNullOrEmpty(identifier.Value))
            {
                messages.Add($"{ClaimNames.RequestingPractitioner}:identifier:[{index}] value' is missing or empty.");
            }
        }

        return messages.Count != 0 ? (false, messages.ToArray()) : (true, messages.ToArray());
    }

    public (bool IsValid, string Message) ValidateRequestingDeviceCommon(RequestingDevice requestingDevice)
    {
        var identifierNode = requestingDevice.Identifier;
        if (identifierNode == null || identifierNode.Length == 0)
        {
            return (false, $"'{ClaimNames.RequestingDevice}' claim is invalid - identifier is missing or empty");
        }

        var warningMessage = "";
        // not mandatory but warning
        if (string.IsNullOrWhiteSpace(requestingDevice.ResourceType))
        {
            warningMessage = $"warning: '{ClaimNames.RequestingDevice}:resource_type' is missing or empty";
        }

        var firstIdentifier = identifierNode.First();

        if (string.IsNullOrWhiteSpace(firstIdentifier.System) ||
            string.IsNullOrWhiteSpace(firstIdentifier.Value) ||
            string.IsNullOrWhiteSpace(requestingDevice.Model) ||
            string.IsNullOrWhiteSpace(requestingDevice.Version) ||
            !IsValidUrl(firstIdentifier.System))
        {
            return string.IsNullOrEmpty(warningMessage)
                ? (false, $"'{ClaimNames.RequestingDevice}' claim is invalid - see GP Connect specification")
                : (false,
                    $"'{ClaimNames.RequestingDevice}' claim is invalid - see GP Connect specification \n {warningMessage}");
        }

        return string.IsNullOrEmpty(warningMessage)
            ? (true, $"'{ClaimNames.RequestingDevice}' claim is valid")
            : (true, $"'{ClaimNames.RequestingDevice}' claim is valid \n {warningMessage}");
    }

    public (bool IsValid, string[] Messages, T? DeserializedClaim) ValidateRequestingOrganizationCommon<T>(
        JwtSecurityToken token) where T : RequestingOrganization
    {
        var claim = token.Claims.FirstOrDefault(x => x.Type == $"{ClaimNames.RequestingOrganization}");

        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return (false, [$"'{ClaimNames.RequestingOrganization}' claim is invalid - claim cannot be null or empty"],
                null);
        }

        try
        {
            List<string> messages = [];
            var deserializedValue = JsonService.DeserializeClaim<T>(claim.Value);

            if (deserializedValue == null)
            {
                return (false, [$"'{ClaimNames.RequestingOrganization}' claim is invalid"], null);
            }

            if (string.IsNullOrWhiteSpace(deserializedValue.ResourceType))
            {
                return (false,
                    [
                        $"'{ClaimNames.RequestingOrganization}:resource_type' claim is invalid - claim cannot be null or empty"
                    ],
                    deserializedValue);
            }

            if (deserializedValue.Identifier == null || deserializedValue.Identifier.Length < 1)
            {
                return (false,
                    [$"'{ClaimNames.RequestingOrganization}' claim is invalid -  claim missing an identifier value"],
                    deserializedValue);
            }

            for (var index = 0; index < deserializedValue.Identifier.Length; index++)
            {
                var identifier = deserializedValue.Identifier[index];
                if (string.IsNullOrWhiteSpace(identifier.System) || string.IsNullOrWhiteSpace(identifier.Value))
                {
                    messages.Add($"'{ClaimNames.RequestingOrganization}:identifier[{index}]' claim is invalid");
                }
            }

            return messages.Count > 0
                ? (false, messages.ToArray(), deserializedValue)
                : (true, [$"'{ClaimNames.RequestingOrganization}' claim is valid"], deserializedValue);
        }
        catch (JsonException jsonException)
        {
            return (false, [jsonException.Message], null);
        }
    }

    private bool IsValidUrl(string url)
    {
        const string pattern = @"^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$";
        return Regex.IsMatch(url, pattern);
    }
}