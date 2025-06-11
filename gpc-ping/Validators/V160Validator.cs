using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace gpc_ping.Validators;

public class V160Validator(JwtSecurityToken token, IValidationCommonValidation validationHelper)
    : BaseValidator(token, validationHelper)
{
    public override (bool IsValid, string Message) ValidateReasonForRequest()
    {
        var reason = token.Claims.FirstOrDefault(x => x.Type == "reason_for_request")?.Value;

        if (string.IsNullOrEmpty(reason))
            return (false, "Missing 'reason_for_request' claim");

        return reason is "directcare" or "migration"
            ? (true, "'reason_for_request' is valid")
            : (false, $"Invalid 'reason_for_request': '{reason}'");
    }

    public override (bool IsValid, string Message) ValidateRequestedScope(string[] acceptedClaimValues)
    {
        string[] acceptedConfidentialityValues =
        [
            "conf/N", "conf/R"
        ];

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

        return claimValues.Length switch
        {
            < 1 => (false, "requested_scope claim must have at least 1 value"),
            1 => CheckScopedClaimValues(claimValues, acceptedClaimValues),
            2 => CheckScopedClaimValues(claimValues, acceptedClaimValues),
            _ => (false, "requested_scope claim is  invalid")
        };

        (bool, string) CheckScopedClaimValues(string[] values, string[] acceptedClaimValues1)
        {
            var validScope = acceptedClaimValues.Contains(values.First());
            var validConfidentiality = true;

            if (values.Length == 2)
            {
                validConfidentiality = acceptedConfidentialityValues.Contains(values.Last());
            }

            if (validScope && validConfidentiality)
            {
                return (true, "'requested_scope' claim is valid");
            }

            return (false, $"Invalid 'requested_scope' claim - claim contains {values.Length} invalid value(s)");
        }
    }

    public override (bool IsValid, string Message) ValidateRequestingDevice()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string[] Messages) ValidateRequestingPractitioner()
    {
        throw new NotImplementedException();
    }
}