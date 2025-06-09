using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace gpc_ping;

/// <summary>
/// Base Validator which validates using GP Connect API - Cross organisation audit and provenance
/// </summary>
/// <param name="token">Deserialized JWT string</param>
public abstract class BaseValidator(JwtSecurityToken token)
{
    public (bool IsValid, string Message) ValidateHeader()
    {
        var validAlg = string.Equals(token.Header.Alg, "none", StringComparison.InvariantCultureIgnoreCase);
        var validTyp = string.Equals(token.Header.Typ, "JWT", StringComparison.InvariantCultureIgnoreCase);

        return validAlg && validTyp
            ? (true, "Header is valid")
            : (false, "Header is invalid - check GP Connect specification");
    }

    public (bool IsValid, string Message) ValidateIssuer()
    {
        if (string.IsNullOrWhiteSpace(token.Issuer))
        {
            return (false, "Issuer value cannot be null or empty");
        }

        var isValidUrl = Uri.TryCreate(token.Issuer, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

        return isValidUrl
            ? (true, "Issuer is valid")
            : (false, "Issuer must contain the URL of auth server token endpoint");
    }

    public (bool IsValid, string Message) ValidateSubject(string requestingPractitionerId)
    {
        var subject = token.Claims.SingleOrDefault(x => x.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(subject))
        {
            return (false, "Subject cannot be null or empty");
        }

        var practitionerClaim = token.Claims.SingleOrDefault(x => x.Type == "requesting_practitioner")?.Value;

        if (string.IsNullOrEmpty(practitionerClaim))
        {
            return (false, "Missing 'requesting_practitioner' claim.");
        }

        RequestingPractitioner? practitioner;

        try
        {
            practitioner = JsonSerializer.Deserialize<RequestingPractitioner>(practitionerClaim);
        }
        catch (JsonException)
        {
            return (false, "Invalid JSON in 'requesting_practitioner' claim.");
        }

        if (practitioner == null || string.IsNullOrEmpty(practitioner.Id))
        {
            return (false, "'requesting_practitioner.id' is missing or empty.");
        }

        return !practitioner.Id.Equals(subject)
            ? (false, "Subject and requesting_practitioner.Id mismatch")
            : (true, "Subject is valid.");
    }

    public (bool IsValid, string[] Messages) ValidateLifetime()
    {
        var messages = new List<string>();

        var issuedAtUnix = GetUnixTimeClaim("iat", "Missing 'iat' claim.", "'iat' claim is not a valid Unix time.");
        var expiresAtUnix = GetUnixTimeClaim("exp", "Missing 'exp' claim.", "'exp' claim is not a valid Unix time.");

        if (messages.Count > 0)
            return (false, messages.ToArray());

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix).UtcDateTime;
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime;

        var timeSpan = expiresAt - issuedAt;

        const double toleranceInMinutes = 0.001;
        const int expectedLifetimeMinutes = 5;

        if (Math.Abs(timeSpan.TotalMinutes - expectedLifetimeMinutes) > toleranceInMinutes)
            messages.Add("Lifetime of claim is not valid");

        return messages.Count == 0
            ? (true, ["Lifetime is valid"])
            : (false, messages.ToArray());

        // Local helper method
        long GetUnixTimeClaim(string claimType, string missingMessage, string invalidMessage)
        {
            var value = token.Claims.SingleOrDefault(x => x.Type == claimType)?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                messages.Add(missingMessage);
                return 0;
            }

            if (long.TryParse(value, out var unixTime)) return unixTime;

            messages.Add(invalidMessage);
            return 0;
        }
    }

    public virtual (bool IsValid, string Message) ValidateAudience()
    {
        return token.Audiences.IsNullOrEmpty()
            ? (false, "Audience is not valid - must have value")
            : IsValidAudience(token.Claims.FirstOrDefault(x => x.Type == "aud"));
    }

    public virtual (bool IsValid, string Message) ValidateReasonForRequest()
    {
        var reason =
            token.Claims.FirstOrDefault(x => x.Type == "reason_for_request")?.Value;
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Missing 'reason_for_request' claim");

        // GP Connect only supports usage for direct care on most versions of spec
        return reason == "directcare"
            ? (true, "Reason for request is valid.")
            : (false, $"Invalid reason for request: '{reason}'");
    }

    public virtual (bool IsValid, string Message) ValidateRequestedScope(string[] acceptedClaimValues)
    {
        if (acceptedClaimValues == null || acceptedClaimValues.Length == 0)
        {
            throw new ArgumentException("acceptedClaims must not be null or empty", nameof(acceptedClaimValues));
        }

        var scopeClaim = token.Claims.FirstOrDefault(x => x.Type == "requested_scope");
        if (scopeClaim == null)
        {
            return (false, "Missing 'requested_scope' claim");
        }

        if (string.IsNullOrWhiteSpace(scopeClaim.Value))
        {
            return (false, "'requested_scope' claim cannot be null or empty");
        }

        var claimValues = scopeClaim.Value.Split(' ');

        if (claimValues.Length is < 1 or > 1)
        {
            return (false, "'requested_scope' claim must 1 value");
        }

        return acceptedClaimValues.Contains(claimValues.First())
            ? (true, "'requested_scope' claim is valid")
            : (false, "Invalid 'requested_scope' claim - claim contains invalid value(s)");
    }

    public abstract (bool IsValid, string Message)
        ValidateRequestedRecord();

    public abstract (bool IsValid, string Message)
        ValidateRequestingDevice();

    public abstract (bool IsValid, string Message)
        ValidateRequestingOrganization();

    public abstract (bool IsValid, string Message)
        ValidateRequestingPractitioner();

    private (bool IsValid, string Message) IsValidAudience(Claim? audienceClaim)
    {
        var value = audienceClaim?.Value;
        if (audienceClaim == null || string.IsNullOrWhiteSpace(value))
        {
            return (false, "Audience claim not valid - must have value");
        }

        const string pattern = @"^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$";
        var isValid = Regex.IsMatch(value, pattern);

        return isValid
            ? (true, "Audience is valid")
            : (false, "Audience is not valid - see GP Connect specification");
    }
}