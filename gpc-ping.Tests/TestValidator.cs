using System.IdentityModel.Tokens.Jwt;
using gpc_ping;

public class TestValidator : BaseValidator
{
    public TestValidator(JwtSecurityToken token) : base(token)
    {
    }

    public override (bool, string) ValidateAudience()
    {
        throw new NotImplementedException();
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