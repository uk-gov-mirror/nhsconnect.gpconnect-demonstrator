using System.IdentityModel.Tokens.Jwt;

namespace gpc_ping.Validators;

public class V127Validator(JwtSecurityToken token) : BaseValidator(token)
{
    public override (bool IsValid, string Message) ValidateRequestedRecord()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string Message) ValidateRequestingDevice()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string Message) ValidateRequestingOrganization()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string Message) ValidateRequestingPractitioner()
    {
        throw new NotImplementedException();
    }
}