using System.IdentityModel.Tokens.Jwt;
using gpc_ping;

internal class TestValidator(JwtSecurityToken token) : BaseValidator(token)
{
    // Stubbed Concrete Implementations to isolate testing of base concrete methods
    public override (bool IsValid, string Message) ValidateAudience() => (true, "stub");
    public override (bool IsValid, string Message) ValidateReasonForRequest() => (true, "stub");
    public override (bool, string) ValidateRequestedRecord() => (true, "stub");
    public override (bool, string) ValidateRequestedScope() => (true, "stub");
    public override (bool, string) ValidateRequestingDevice() => (true, "stub");
    public override (bool, string) ValidateRequestingOrganization() => (true, "stub");
    public override (bool, string) ValidateRequestingPractitioner() => (true, "stub");
}