using System.IdentityModel.Tokens.Jwt;

namespace gpc_ping;

public class ValidationService : IValidateService
{
    const string ISSUER_CLAIM = "iss";
    const string SUBJECT_CLAIM = "sub";
    const string AUDIENCE_CLAIM = "aud";
    const string EXPIRY_CLAIM = "exp";
    const string ISSUED_AT_CLAIM = "iat";
    const string REASON_FOR_REQUEST_CLAIM = "reason_for_request";
    const string REQUESTED_RECORD_CLAIM = "requested_record";
    const string REQUESTED_SCOPE_CLAIM = "requested_scope";
    const string REQUESTING_DEVICE_CLAIM = "requesting_device";
    const string REQUESTING_ORGANIZATION_CLAIM = "requesting_organization";
    const string REQUESTING_PRACTITIONER_CLAIM = "requesting_practitioner";

    public static JwtSecurityToken DecodeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("Token has invalid format. Expected 3 segments");
        }

        if (!string.IsNullOrEmpty(parts[2]))
        {
            throw new ArgumentException("Token should not be signed");
        }

        return new JwtSecurityTokenHandler().ReadJwtToken(token);
    }


    public (bool IsValid, string[] Issues) ValidateMandatoryClaimNames(JwtSecurityToken token, string requestMethod)
    {
        var mandatoryClaims = new[]
        {
            ISSUER_CLAIM, SUBJECT_CLAIM, AUDIENCE_CLAIM,
            EXPIRY_CLAIM, ISSUED_AT_CLAIM, REASON_FOR_REQUEST_CLAIM,
            REQUESTED_RECORD_CLAIM, REQUESTED_SCOPE_CLAIM,
            REQUESTING_DEVICE_CLAIM, REQUESTING_ORGANIZATION_CLAIM,
            REQUESTING_PRACTITIONER_CLAIM
        };

        var missingClaims = mandatoryClaims
            .Where(claim => !token.Claims.Any(c => c.Type == claim))
            .ToArray();

        return missingClaims.Length > 0 ? (false, missingClaims) : (true, []);
    }

    public (bool IsValid, string[] Issues) ValidateMandatoryClaimValues(JwtSecurityToken token, string version,
        string requestMethod)
    {
        throw new NotImplementedException();
    }

    public (bool IsValid, string[] Issues) ValidateOptionalClaims(JwtSecurityToken token, string requestMethod,
        string version)
    {
        throw new NotImplementedException();
    }
}