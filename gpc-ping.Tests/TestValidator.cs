using System.IdentityModel.Tokens.Jwt;
using gpc_ping;
using gpc.Helpers;

internal class TestValidator : BaseValidator
{
    public TestValidator(JwtSecurityToken token, IValidationCommonValidation validation) : base(token, validation)
    {
    }

    // Stubbed Concrete Implementations to isolate testing of base concrete methods
    public override (bool IsValid, string[] Messages) ValidateRequestingPractitioner() => (true, ["stub"]);
}