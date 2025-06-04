using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gpc_ping.Extensions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace gpc;

public class ValidateMandatoryClaimsTests
{
    /*
     * {
         "sub": "user_id_123",
         "name": "Tester",
         "scope": "api.read user.read",
         "iat": 1717448000,   // issued at
         "nbf": 1717448000,   // not before
         "exp": 1748568000,   // expires far in the future
         "iss": "auth.service.com",
         "aud": "my-audience"
       }
     */


    [Fact]
    public void ValidToken_ReturnsNoIssues()
    {
        // Arrange
        const string validToken =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTc0OTA0OTk5OSwiZXhwIjoxNzQ5MDUzNTk5fQ.";

        var token = new JwtSecurityTokenHandler().ReadJwtToken(validToken);


        // Act
        var result = token.ValidateToken(requestMethod: "POST", new FakeLogger<Program>());

        // Assert
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void Missing_Iss_ReportsIssue()
    {
        // Arrange

        // Act

        // Assert
    }


    private record TestTokenData(
        string Audience,
        string Issuer,
        DateTime Expiration,
        DateTime IssuedAt,
        string ReasonForRequest,
        string? Scope = "patient /*.read",
        string? Subject = "https://api.service.nhs.uk/gpconnect",
        string? RequestingSystem = "urn:nhs:nrls:requesting-system",
        string? RequestingOrganisation = "ORG123",
        string? RequestingUser = "urn:nhs:person:user-1234",
        string? RequestingPatient = "9999999999"
    );


    private static string GenerateTestToken(TestTokenData tokenData)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new Claim("iss", tokenData.Issuer),
            new Claim("sub", tokenData.Subject),
            new Claim("aud", tokenData.Audience),
            new Claim("exp", ToUnixTime(tokenData.Expiration).ToString()),
            new Claim("iat", ToUnixTime(tokenData.IssuedAt).ToString()),
            new Claim("reason_for_request", tokenData.ReasonForRequest),
            new Claim("scope", tokenData.Scope),
            new Claim("requesting_system", tokenData.RequestingSystem)
        };

        if (string.IsNullOrEmpty(tokenData.RequestingOrganisation))
            claims.Add(new Claim("requesting_organization", tokenData.RequestingOrganisation));

        if (!string.IsNullOrEmpty(tokenData.RequestingUser))
            claims.Add(new Claim("requesting_user", tokenData.RequestingUser));

        if (string.IsNullOrEmpty(tokenData.RequestingPatient))
            claims.Add(new Claim("requesting_patient", tokenData.RequestingPatient));

        // Key only needed if you're verifying signature (use dummy if not verifying)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-not-used"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "https://requesting-system.example.nhs.uk",
            audience: "https://api.service.nhs.uk/gpconnect",
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static long ToUnixTime(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
}