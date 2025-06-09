using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gpc_ping.Validators;

namespace gpc.Helpers;

public static class TestHelpers
{
    public static JwtSecurityToken CreateTokenWithClaims(Dictionary<string, string> claimsDict)
    {
        var claims = claimsDict.Select(kvp => new Claim(kvp.Key, kvp.Value)).ToList();

        return new JwtSecurityToken(claims: claims);
    }

    public static TValidator CreateValidatorWithToken<TValidator>(Dictionary<string, string> claims)
        where TValidator : class
    {
        var token = CreateTokenWithClaims(claims);

        if (Activator.CreateInstance(typeof(TValidator), token) is not TValidator validator)
        {
            throw new InvalidOperationException(
                $"Cannot create an instance of {typeof(TValidator).Name} with a JwtSecurityToken.");
        }

        return validator;
    }
}