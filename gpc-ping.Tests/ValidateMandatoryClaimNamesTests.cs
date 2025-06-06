using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gpc_ping;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace gpc;

/* mandatory claim keys

     iss (issuer)
     sub (subject)
     aud (audience)
     exp (expiry)
     iat (issued at)
     reason_for_request
     requested_record
     requested_scope
     requesting_device
     requesting_organization
     requesting_practitioner

 */

public class ValidateMandatoryClaimNamesTests
{
    [Fact]
    public void Returns_Valid_When_AllMandatoryClaimsPresent()
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(),
                ClaimValueTypes.Integer64),

            new Claim("sub", "subject"),
            new Claim("reason_for_request", "request reason"),
            new Claim("requested_record", "record"),
            new Claim("requested_scope", "scope"),
            new Claim("requesting_device", "device"),
            new Claim("requesting_organization", "organization"),
            new Claim("requesting_practitioner", "practitioner")
        };

        var token = new JwtSecurityToken(
            issuer: "issuer",
            audience: "https://authorize.fhir.nhs.net/token",
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(5)
        );


        // Act
        var response = new ValidationService().ValidateMandatoryClaimNames(token, "GET");

        // Assert
        response.Issues.ShouldBeEmpty();
        response.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Returns_InValid_When_MissingMandatoryClaims()
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken();

        // Act
        var response = new ValidationService().ValidateMandatoryClaimNames(token, "GET");

        // Assert
        response.Issues.ShouldNotBeEmpty();
        response.Issues.Length.ShouldBe(11); // All mandatory claims are missing
        response.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Returns_InValid_When_MissingSomeMandatoryClaims()
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new Claim("sub", "subject"),
            new Claim("reason_for_request", "request reason"),
            new Claim("requesting_organization", "organization"),
            new Claim("requesting_practitioner", "practitioner")
        };

        var token = new JwtSecurityToken(
            issuer: "issuer",
            audience: "https://authorize.fhir.nhs.net/token",
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(5)
        );


        // Act
        var response = new ValidationService().ValidateMandatoryClaimNames(token, "GET");

        // Assert
        response.Issues.ShouldNotBeEmpty();
        response.Issues.Length.ShouldBe(4); // Missing 4 mandatory claims
        response.Issues.ShouldContain("iat");
        response.Issues.ShouldContain("requested_record");
        response.Issues.ShouldContain("requested_scope");
        response.Issues.ShouldContain("requesting_device");
        response.IsValid.ShouldBeFalse();
    }
}