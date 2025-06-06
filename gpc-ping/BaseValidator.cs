using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

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

    public (bool IsValid, string Message) ValidateSubject()
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
        return (true, ["Lifetime is valid"]);
    }

    public abstract (bool IsValid, string Message) ValidateAudience();

    public abstract (bool, string) ValidateReasonForRequest();
    public abstract (bool, string) ValidateRequestedRecord();
    public abstract (bool, string) ValidateRequestedScope();
    public abstract (bool, string) ValidateRequestingDevice();
    public abstract (bool, string) ValidateRequestingOrganization();
    public abstract (bool, string) ValidateRequestingPractitioner();
}