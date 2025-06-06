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

    public override (bool, string) ValidateExpiration()
    {
        throw new NotImplementedException();
    }

    public override (bool, string) ValidateIssuedAt()
    {
        throw new NotImplementedException();
    }

    public override (bool, string) ValidateReasonForRequest()
    {
        throw new NotImplementedException();
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