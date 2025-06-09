using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace gpc_ping.Validators;

public class V074Validator(JwtSecurityToken token) : BaseValidator(token)
{
    public override (bool IsValid, string Message) ValidateAudience()
    {
        var audience = token.Audiences.FirstOrDefault();
        if (audience == null || string.IsNullOrWhiteSpace(audience))
        {
            return (false, "Audience is not valid - must have value");
        }

        var value = token.Audiences.FirstOrDefault();

        return value == "https://authorize.fhir.nhs.net/token"
            ? (true, "Audience is valid")
            : (false, "Audience is not valid - see GP Connect specification");
    }

    public override (bool, string) ValidateRequestedRecord()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string Message) ValidateRequestedScope(string[] acceptedClaimValues)
    {
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
            return (false, "requested_scope claim must 1 value");
        }

        return acceptedClaimValues.Contains(claimValues.First())
            ? (true, "'requested_scope' claim is valid")
            : (false,
                "Invalid 'requested_scope' claim - claim contains {invalidValueCount} invalid value(s)");


        return (true, "Requested scope is valid");
    }

    public override (bool, string) ValidateRequestingDevice()
    {
        throw new NotImplementedException();
    }

    public override (bool, string) ValidateRequestingOrganization()
    {
        throw new NotImplementedException();
    }

    public override (bool, string) ValidateRequestingPractitioner()
    {
        throw new NotImplementedException();
    }
}