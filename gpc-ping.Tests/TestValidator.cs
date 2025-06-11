using System.IdentityModel.Tokens.Jwt;
using gpc_ping;

internal class TestValidator(JwtSecurityToken token) : BaseValidator(token)
{
    // Stubbed Concrete Implementations to isolate testing of base concrete methods
    public override (bool IsValid, string Message) ValidateRequestingOrganization() => (true, "stub");
    public override (bool IsValid, string[] Messages) ValidateRequestingPractitioner() => (true, ["stub"]);
}