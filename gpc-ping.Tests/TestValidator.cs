using System.IdentityModel.Tokens.Jwt;
using gpc_ping;

internal class TestValidator : BaseValidator
{
    public TestValidator(JwtSecurityToken token, IValidationCommonValidation validation) : base(token, validation)
    {
    }

    // stubbed override
    public override (bool IsValid, string[] Messages) Validate() => (true, []);
}