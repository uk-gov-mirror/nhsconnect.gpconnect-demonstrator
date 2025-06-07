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

    public override (bool IsValid, string Message) ValidateReasonForRequest()
    {
        var reason = token.Claims.FirstOrDefault(x => x.Type == "reason_for_request")?.Value;

        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Missing 'reason_for_request' claim");

        // GP Connect only supports usage for direct care on this version of spec
        return reason == "directcare"
            ? (true, "Reason for request is valid.")
            : (false, $"Invalid reason for request: '{reason}'");
    }

    public override (bool, string) ValidateRequestedRecord()
    {
        throw new NotImplementedException();
    }

    public override (bool, string) ValidateRequestedScope()
    {
        throw new NotImplementedException();
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