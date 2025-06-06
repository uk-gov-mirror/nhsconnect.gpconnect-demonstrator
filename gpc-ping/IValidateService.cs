using System.IdentityModel.Tokens.Jwt;

namespace gpc_ping;

public interface IValidateService
{
    (bool IsValid, string[] Issues) ValidateMandatoryClaimNames(JwtSecurityToken token, string requestMethod);

    (bool IsValid, string[] Issues) ValidateMandatoryClaimValues(
        JwtSecurityToken token,
        string version,
        string requestMethod
    );

    (bool IsValid, string[] Issues) ValidateOptionalClaims(
        JwtSecurityToken token,
        string requestMethod,
        string version
    );
}