using System.IdentityModel.Tokens.Jwt;

namespace gpc_ping.Validators;

public class V127Validator(JwtSecurityToken token, IValidationCommonValidation validationHelper)
    : BaseValidator(token, validationHelper)
{
    public override (bool IsValid, string Message) ValidateRequestingDevice()
    {
        throw new NotImplementedException();
    }

    public override (bool IsValid, string[] Messages) ValidateRequestingPractitioner()
    {
        throw new NotImplementedException();
    }
}