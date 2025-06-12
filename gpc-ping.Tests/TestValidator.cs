using System.IdentityModel.Tokens.Jwt;
using gpc_ping;
using gpc.Helpers;

internal class TestValidator : BaseValidator
{
    public TestValidator(JwtSecurityToken token, IValidationCommonValidation validation) : base(token, validation)
    {
    }
}