using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace gpc_ping.Validators;

public static class ValidationHelpers
{
    /// <summary>
    /// Deserializes RequestingPractitioner claim, and validates common properties across spec versions
    /// </summary>
    /// <param name="requestingPractitionerClaim"></param>
    /// <returns></returns>
    public static (bool IsValid, string[] Messages, RequestingPractitioner? requestingPractitioner)
        DeserializeAndValidateCommonRequestingPractitionerProperties(
            Claim? requestingPractitionerClaim)
    {
        if (requestingPractitionerClaim == null)
        {
            return (false, ["Missing 'requesting_practitioner' claim"], null);
        }

        if (string.IsNullOrEmpty(requestingPractitionerClaim.Value))
            return (false, ["'requesting_practitioner' value cannot be null or empty"], null);

        RequestingPractitioner? parsedPractitioner;
        try
        {
            parsedPractitioner = JsonSerializer.Deserialize<RequestingPractitioner>(requestingPractitionerClaim.Value);
        }
        catch (JsonException)
        {
            return (false, ["Invalid JSON in 'requesting_practitioner' claim."], null);
        }

        if (parsedPractitioner == null || string.IsNullOrEmpty(parsedPractitioner.Id))
        {
            return (false, ["'requesting_practitioner.id' is missing or empty."], parsedPractitioner);
        }

        // name:
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

    public static (bool IsValid, string[] Messages) ValidateRequestingPractitionerIdentifier(
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
                messages.Add($"identifier:[{index}] system' is missing or empty.");
            }

            if (string.IsNullOrEmpty(identifier.Value))
            {
                messages.Add($"identifier:[{index}] value' is missing or empty.");
            }
        }

        return messages.Count != 0 ? (false, messages.ToArray()) : (true, messages.ToArray());
    }


    public static (bool IsValid, string Message) ValidateRequestingDeviceCommon(RequestingDevice requestingDevice)
    {
        var identifierNode = requestingDevice.Identifier;
        if (identifierNode == null || identifierNode.Length == 0)
        {
            return (false, "Invalid requesting device - see GP Connect specification");
        }

        var warningMessage = "";
        // not mandatory but warning
        if (string.IsNullOrWhiteSpace(requestingDevice.ResourceType))
        {
            warningMessage = "warning: resource_type is missing or empty.";
        }

        var firstIdentifier = identifierNode.First();

        if (string.IsNullOrWhiteSpace(firstIdentifier.System) ||
            string.IsNullOrWhiteSpace(firstIdentifier.Value) ||
            string.IsNullOrWhiteSpace(requestingDevice.Model) ||
            string.IsNullOrWhiteSpace(requestingDevice.Version) ||
            !ValidationHelpers.IsValidUrl(firstIdentifier.System))
        {
            return string.IsNullOrEmpty(warningMessage)
                ? (false, "Invalid requesting device - see GP Connect specification")
                : (false, $"Invalid requesting device - see GP Connect specification \n {warningMessage}");
        }

        return string.IsNullOrEmpty(warningMessage)
            ? (true, "The requesting device is valid.")
            : (true, $"The requesting device is valid. \n {warningMessage}");
    }


    private static bool IsValidUrl(string url)
    {
        const string pattern = @"^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$";
        return Regex.IsMatch(url, pattern);
    }
}