using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace gpc_ping;

/// <summary>
/// Base Validator which validates using GP Connect API - Cross organisation audit and provenance
/// </summary>
/// <param name="token">Deserialized JWT string</param>
public abstract class BaseValidator
{
    protected IValidationCommonValidation _validationHelper;

    protected BaseValidator(JwtSecurityToken token, IValidationCommonValidation validationHelper)
    {
        Token = token ?? throw new ArgumentNullException(nameof(token));
        _validationHelper = validationHelper;
    }

    private string[] BASE_SCOPES =
    [
        "patient/*.read", "patient/*.write", "organization/*.read", "organisation/*.write"
    ];

    public abstract (bool IsValid, string[] Messages) Validate();

    protected (bool IsValid, string[] Messages) ValidateAll(string[] acceptedScopes)
    {
        var results = new List<(bool IsValid, string Message)>
        {
            ValidateHeader(),
            ValidateIssuer(),
            ValidateAudience(),
            ValidateReasonForRequest(),
            ValidateRequestedScope(acceptedScopes),
            ValidateRequestingDevice()
        };

        var (lifetimeValid, lifetimeMessages) = ValidateLifetime();
        results.Add((lifetimeValid, string.Join("; ", lifetimeMessages)));

        var (organizationValid, organizationMessages) = ValidateRequestingOrganization();
        results.Add((organizationValid, string.Join("; ", organizationMessages)));

        var (practitionerValid, practitionerMessages) = ValidateRequestingPractitioner();
        results.Add((practitionerValid, string.Join("; ", practitionerMessages)));

        string? practitionerId = null;
        if (practitionerValid)
        {
            var practitionerJson = Token.Claims.FirstOrDefault(c => c.Type == "requesting_practitioner")?.Value;
            if (!string.IsNullOrEmpty(practitionerJson))
            {
                var deserializedObject = JsonService.DeserializeClaim<RequestingPractitioner>(practitionerJson);
                practitionerId = deserializedObject?.Id;
            }
        }

        var (subjectValid, subjectMessage) = ValidateSubject(practitionerId);
        results.Add((subjectValid, subjectMessage));

        var isValid = results.All(r => r.IsValid);
        string[] messages = results.Select(x => x.Message).ToArray();
        return (isValid, messages);
    }

    public JwtSecurityToken Token { get; }

    public (bool IsValid, string Message) ValidateHeader()
    {
        var validAlg = string.Equals(Token.Header.Alg, "none", StringComparison.InvariantCultureIgnoreCase);
        var validTyp = string.Equals(Token.Header.Typ, "JWT", StringComparison.InvariantCultureIgnoreCase);

        return validAlg && validTyp
            ? (true, "Header is valid")
            : (false, "Header is invalid - check GP Connect specification");
    }

    public (bool IsValid, string Message) ValidateIssuer()
    {
        if (string.IsNullOrWhiteSpace(Token.Claims.FirstOrDefault(c => c.Type == ClaimNames.Issuer)?.Value))
        {
            return (false, "Issuer value cannot be null or empty");
        }

        var isValidUrl = Uri.TryCreate(Token.Issuer, UriKind.Absolute, out var uri);

        return isValidUrl
            ? (true, $"'{ClaimNames.Issuer}' claim is valid")
            : (false,
                $"'{ClaimNames.Issuer} claim is invalid -  claim must contain the URL of auth server token endpoint");
    }

    public (bool IsValid, string Message) ValidateSubject(string? requestingPractitionerId)
    {
        if (requestingPractitionerId == null)
        {
            return (false, $"'{ClaimNames.Subject}' claim is invalid - 'requesting_practitioner:id' is null");
        }

        var subject = Token.Claims.SingleOrDefault(x => x.Type == ClaimNames.Subject)?.Value;

        if (string.IsNullOrEmpty(subject))
        {
            return (false, "Subject cannot be null or empty");
        }

        return !requestingPractitionerId.Equals(subject)
            ? (false,
                $"'{ClaimNames.Subject}' claim is invalid - {ClaimNames.Subject}:id and {ClaimNames.RequestingPractitioner}:id mismatch")
            : (true, $"'{ClaimNames.Subject}' claim is valid.");
    }

    public virtual (bool IsValid, string Message) ValidateAudience()
    {
        return Token.Claims.FirstOrDefault(x => x.Type == ClaimNames.Audience)?.Value == null
            ? (false, $"'{ClaimNames.Audience}' claim cannot be null or empty")
            : IsValidAudience(Token.Claims.FirstOrDefault(x => x.Type == $"{ClaimNames.Audience}"));
    }

    /// <summary>
    /// Validates both 'iat' and 'exp' claims
    /// </summary>
    /// <returns></returns>
    public (bool IsValid, string[] Messages) ValidateLifetime()
    {
        var messages = new List<string>();

        var issuedAtUnix = GetUnixTimeClaim(ClaimNames.IssuedAt, $"Missing '{ClaimNames.IssuedAt}' claim.",
            $"'{ClaimNames.IssuedAt}' claim is not a valid Unix time.");

        var expiresAtUnix = GetUnixTimeClaim(ClaimNames.Expiration, $"Missing '{ClaimNames.Expiration}' claim.",
            $"'{ClaimNames.Expiration}' claim is not a valid Unix time.");

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
            ? (true, ["Lifetime of claim is valid"])
            : (false, messages.ToArray());

        // Local helper method
        long GetUnixTimeClaim(string claimType, string missingMessage, string invalidMessage)
        {
            var value = Token.Claims.SingleOrDefault(x => x.Type == claimType)?.Value;

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


    public virtual (bool IsValid, string Message) ValidateReasonForRequest()
    {
        var reason =
            Token.Claims.FirstOrDefault(x => x.Type == ClaimNames.ReasonForRequest)?.Value;
        if (string.IsNullOrWhiteSpace(reason))
            return (false,
                $"'{ClaimNames.ReasonForRequest}' is invalid - Missing '{ClaimNames.ReasonForRequest}' claim");

        // GP Connect only supports usage for direct care on most versions of spec
        return reason == "directcare"
            ? (true, $"'{ClaimNames.ReasonForRequest}' is valid.")
            : (false, $"Invalid '{ClaimNames.ReasonForRequest}': '{reason}'");
    }

    public virtual (bool IsValid, string Message) ValidateRequestedScope(string[] acceptedClaimValues)
    {
        if (acceptedClaimValues == null || acceptedClaimValues.Length == 0)
        {
            throw new ArgumentException("acceptedClaims must not be null or empty", nameof(acceptedClaimValues));
        }

        var scopeClaim = Token.Claims.FirstOrDefault(x => x.Type == $"{ClaimNames.RequestedScope}");
        if (scopeClaim == null)
        {
            return (false, $"'{ClaimNames.RequestedScope}' claim is invalid - missing '{ClaimNames.RequestedScope}' claim");
        }

        if (string.IsNullOrWhiteSpace(scopeClaim.Value))
        {
            return (false, $"'{ClaimNames.RequestedScope}' claim cannot be null or empty");
        }

        var claimValues = scopeClaim.Value.Split(' ');

        if (claimValues.Length is < 1 or > 1)
        {
            return (false, $"'{ClaimNames.RequestedScope}' claim must 1 value");
        }

        return acceptedClaimValues.Contains(claimValues.First())
            ? (true, $"'{ClaimNames.RequestedScope}' claim is valid")
            : (false, $"'{ClaimNames.RequestedScope}' claim is invalid - claim contains invalid value(s)");
    }


    public virtual (bool IsValid, string Message) ValidateRequestingDevice()
    {
        var claim = Token.Claims.FirstOrDefault(x => x.Type == $"{ClaimNames.RequestingDevice}");

        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return (false, $"'{ClaimNames.RequestingDevice}' claim is invalid - claim cannot be null or empty");
        }

        var requestingDevice = JsonService.DeserializeClaim<RequestingDevice>(claim.Value);

        return requestingDevice == null
            ? (false, $"'{ClaimNames.RequestingDevice}' claim is invalid - see GP Connect specification")
            : _validationHelper.ValidateRequestingDeviceCommon(requestingDevice);
    }

    public virtual (bool IsValid, string[] Messages) ValidateRequestingOrganization()
    {
        var (isValid, messages, _) =
            _validationHelper.ValidateRequestingOrganizationCommon<RequestingOrganization>(Token);

        return (isValid, messages);
    }

    public virtual (bool IsValid, string[] Messages) ValidateRequestingPractitioner()
    {
        var (isValid, messages, deserializedRequestingPractitioner) =
            _validationHelper.DeserializeAndValidateCommonRequestingPractitionerProperties<RequestingPractitioner>(
                Token.Claims.FirstOrDefault(x => x.Type == $"{ClaimNames.RequestingPractitioner}"));

        if (!isValid || deserializedRequestingPractitioner == null)
        {
            return (false, messages);
        }

        const int requiredIdentifierLength = 3;

        var (isIdentifierValid, identifierMessages) =
            _validationHelper.ValidateRequestingPractitionerIdentifier(deserializedRequestingPractitioner,
                requiredIdentifierLength);

        return !isIdentifierValid
            ? (false, identifierMessages)
            : (true, [$"'{ClaimNames.RequestingPractitioner} claim is valid"]);
    }

    private static (bool IsValid, string Message) IsValidAudience(Claim? audienceClaim)
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