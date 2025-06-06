using System.IdentityModel.Tokens.Jwt;
using gpc_ping;
using Shouldly;

namespace gpc;

public class ValidationServiceTokenDecodingTests
{
    /*
     Valid unsigned token
     Null/empty input
     Signed token
     Too few/too many segments
     Base64 decoding errors
     Malformed JSON in payload
     */

    [Fact]
    public void Decode_ValidToken_WithoutSignature_ReturnsExpectedClaims()
    {
        // Arrange
        const string tokenString =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0." +
            "eyJpYXQiOjE3MTc2NzA0MDAsInN1YiI6InN1YmplY3QiLCJyZWFzb25fZm9yX3JlcXVlc3QiOiJyZXF1ZXN0IHJlYXNvbiIsInJlcXVlc3RlZF9yZWNvcmQiOiJyZWNvcmQiLCJyZXF1ZXN0ZWRfc2NvcGUiOiJzY29wZSIsInJlcXVlc3RpbmdfZGV2aWNlIjoiZGV2aWNlIiwicmVxdWVzdGluZ19vcmdhbml6YXRpb24iOiJvcmdhbml6YXRpb24iLCJyZXF1ZXN0aW5nX3ByYWN0aXRpb25lciI6InByYWN0aXRpb25lciIsImlzcyI6Imlzc3VlciIsImF1ZCI6Imh0dHBzOi8vYXV0aG9yaXplLmZoaXIubmhzLm5ldC90b2tlbiIsIm5iZiI6MTcxNzY3MDQwMCwiZXhwIjoxNzE3NjcwNzAwfQ.";

        var expectedClaims = new Dictionary<string, string>
        {
            ["sub"] = "subject",
            ["reason_for_request"] = "request reason",
            ["requested_record"] = "record",
            ["requested_scope"] = "scope",
            ["requesting_device"] = "device",
            ["requesting_organization"] = "organization",
            ["requesting_practitioner"] = "practitioner",
            ["iss"] = "issuer",
            ["aud"] = "https://authorize.fhir.nhs.net/token",
            ["iat"] = "1717670400",
            ["nbf"] = "1717670400",
            ["exp"] = "1717670700"
        };

        // Act
        var response = ValidationService.DecodeToken(tokenString);

        // Assert
        response.ShouldNotBeNull();
        foreach (var kvp in expectedClaims)
        {
            response.Claims.FirstOrDefault(c => c.Type == kvp.Key)?.Value.ShouldBe(kvp.Value);
        }
    }

    [Fact]
    public void Decode_NullToken_ThrowsArgumentException()
    {
        string token = null!;
        var ex = Should.Throw<ArgumentException>(() => ValidationService.DecodeToken(token));
        ex.Message.ShouldBe("Token cannot be null or empty. (Parameter 'token')");
    }

    [Fact]
    public void Decode_EmptyToken_ThrowsArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => ValidationService.DecodeToken(""));
        ex.Message.ShouldBe("Token cannot be null or empty. (Parameter 'token')");
    }

    [Fact]
    public void Decode_SignedToken_ThrowsArgumentException()
    {
        const string token = "header.payload.signature";
        var ex = Should.Throw<ArgumentException>(() => ValidationService.DecodeToken(token));
        ex.Message.ShouldBe("Token should not be signed");
    }

    [Theory]
    [InlineData("a.b")] // too few segments
    [InlineData("a.b.c.d")] // too many segments
    public void Decode_TokenWithInvalidSegmentCount_ThrowsInvalidOperationException(string token)
    {
        var ex = Should.Throw<InvalidOperationException>(() => ValidationService.DecodeToken(token));
        ex.Message.ShouldContain("Token has invalid format. Expected 3 segments");
    }

    [Fact]
    public void Decode_TokenWithNonBase64Header_ThrowsArgumentException()
    {
        const string token = "asada." +
                             "eyJpYXQiOjE3MTc2NzA0MDAsInN1YiI6IlVzZXIiLCJpc3MiOiJJc3N1ZXIiLCJhdWQiOiJBdWQiLCJuYmYiOjE3MTc2NzA0MDAsImV4cCI6MTcxNzY3MDcwMH0.";
        var ex = Should.Throw<ArgumentException>(() => ValidationService.DecodeToken(token));

        ex.Message.ShouldContain("Unable to decode the header");
        ex.Message.ShouldContain("as Base64Url encoded string");
    }

    [Fact]
    public void Decode_TokenWithNonBase64Payload_ThrowsArgumentException()
    {
        const string token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.1sj2.";
        var ex = Should.Throw<ArgumentException>(() => ValidationService.DecodeToken(token));

        ex.Message.ShouldContain("Unable to decode the payload");
        ex.Message.ShouldContain("as Base64Url encoded string");
    }

    [Fact]
    public void Decode_TokenWithNonJsonPayload_ThrowsArgumentException()
    {
        // base64 of "notjson" = bm90anNvbg
        const string token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.bm90anNvbg.";
        var ex = Should.Throw<ArgumentException>(() => ValidationService.DecodeToken(token));

        ex.InnerException?.Message.ShouldContain("is an invalid JSON literal");
    }
}