using System.IdentityModel.Tokens.Jwt;

namespace gpc_ping.Validators;

public class V127Validator : BaseValidator
{
    public V127Validator(JwtSecurityToken token, IValidationCommonValidation validationHelper) : base(token,
        validationHelper)
    {
    }

    public override (bool IsValid, string[] Messages) Validate()
    {
        return ValidateAll(StaticValues.BaseScopes);
    }
}