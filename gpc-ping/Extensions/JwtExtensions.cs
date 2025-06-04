using System.IdentityModel.Tokens.Jwt;

namespace gpc_ping.Extensions;

/// <summary>
/// Offers a set of static extension methods for performing operations
/// on JSON Web Tokens (JWTs), such as validation, claim extraction,
/// and logging token information.
/// </summary>
public static class JwtExtensions
{
    private static readonly string[] WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];
    private static readonly string[] ReadMethods = ["GET"];

    /// <summary>
    /// Validates the provided JWT token, logs its details, and checks for any issues based on specific rules and claims.
    /// </summary>
    /// <param name="jwtToken">The JWT token to be validated.</param>
    /// <param name="logger">An instance of <see cref="ILogger"/> used to log token details and issues.</param>
    /// <param name="requestMethod">The HTTP request method associated with the token validation context.</param>
    /// <returns>An array of strings representing any issues found during validation, or an empty array if no issues are found.</returns>
    public static string[] ValidateToken(this JwtSecurityToken jwtToken, string requestMethod, ILogger<Program> logger)
    {
        logger.LogInformation("JWT: {jwtToken}", jwtToken);
        logger.LogInformation("JWT Token Header: {Header}", jwtToken.Header);
        logger.LogInformation("JWT Token Issuer: {Issuer}", jwtToken.Issuer);
        logger.LogInformation("JWT Token Valid From: {ValidFrom}", jwtToken.ValidFrom);
        logger.LogInformation("JWT Token Valid To: {ValidTo}", jwtToken.ValidTo);

        logger.LogInformation("JWT Token Claims:");
        foreach (var claim in jwtToken.Claims)
        {
            logger.LogInformation("JWT Claim - {Type}: {Value}", claim.Type, claim.Value);
        }

        logger.LogInformation("----------------JWT End----------------");

        var issues = new List<string>();


        if (jwtToken.ValidTo < DateTime.UtcNow)
            issues.Add("Token has expired");


        ValidateActiveToken(jwtToken, issues);

        ValidateMandatoryClaims(jwtToken, issues);

        if (ValidateRequestingClaims(jwtToken, issues, out var strings))
        {
            return strings;
        }

        // Its confusing either this should be requested_scope or scope,
        // but it is scope in the spec and requested_scope in the demonstrator app.
        ValidateRequestedScopeClaims(jwtToken, requestMethod, issues);


        ValidateRequestingDevice(jwtToken, issues);
        ValidateRequestingPractitioner(jwtToken, issues);

        return [.. issues];
    }

    /// <summary>
    /// Validates the presence and correctness of mandatory claims in the provided JWT token.
    /// </summary>
    /// <param name="jwtToken">The JWT token to be validated for mandatory claims.</param>
    /// <param name="issues">A list of strings to collect validation issues found with the mandatory claims.</param>
    private static void ValidateMandatoryClaims(JwtSecurityToken jwtToken, List<string> issues)
    {
        if (string.IsNullOrEmpty(jwtToken.Issuer))
            issues.Add("Missing mandatory claim: 'iss' (Issuer)");

        if (jwtToken.ValidFrom != default)
            issues.Add("iat is missing / default value");

        if (jwtToken.Claims.All(c => c.Type != "sub"))
            issues.Add("Missing mandatory claim: 'sub' (Subject)");

        if (string.IsNullOrEmpty(jwtToken.Audiences.FirstOrDefault()))
            issues.Add("Missing mandatory claim: 'aud' (Audience)");


        if (jwtToken.Claims.All(c => c.Type != "reason_for_request"))
            issues.Add("Missing mandatory claim: 'reason_for_request'");
        else
        {
            var reasonForRequest = jwtToken.Claims.FirstOrDefault(c => c.Type == "reason_for_request")?.Value;
            if (reasonForRequest != "directcare" && reasonForRequest != "secondaryuses" &&
                reasonForRequest != "patientaccess")
                issues.Add(
                    "Invalid 'reason_for_request' value. Must be one of: directcare, secondaryuses, patientaccess");
        }

        if (jwtToken.Claims.All(c => c.Type != "scope")
            || jwtToken.Claims.All(c => c.Type != "reqested_scope"))
            issues.Add("Missing mandatory claim: 'scope' or 'reqested_scope'");
    }

    /// <summary>
    /// Validates the claims of the provided JWT token to ensure the presence of mandated fields and proper formatting.
    /// Adds descriptive issues to the list if mandatory claims are missing or improperly formatted.
    /// </summary>
    /// <param name="jwtToken">The JWT token containing claims to validate.</param>
    /// <param name="issues">A list to which validation issues are added if found during the claim checks.</param>
    /// <param name="strings">An output array containing potential matching claims if validation passes.</param>
    /// <returns>True if the validation passes successfully; otherwise, false.</returns>
    private static bool ValidateRequestingClaims(JwtSecurityToken jwtToken, List<string> issues, out string[] strings)
    {
        if (jwtToken.Claims.All(c => c.Type != "requesting_system"))
            issues.Add("Missing mandatory claim: 'requesting_system'");

        // Check if the token has a valid sub claim that matches requesting_user/requesting_patient/requesting_system
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        // 'sub' claim is mandatory
        if (string.IsNullOrEmpty(subClaim))
        {
            issues.Add("The 'sub' claim is mandatory and cannot be empty");
            strings = issues.ToArray();
            return true;
        }

        // commented out as this is for care connect not GP Connect requirements. (needs double checking)

        // // Get potential matching claims
        // var requestingUser = jwtToken.Claims.FirstOrDefault(c => c.Type == "requesting_user")?.Value;
        // var requestingPatient = jwtToken.Claims.FirstOrDefault(c => c.Type == "requesting_patient")?.Value;
        // var requestingSystem = jwtToken.Claims.FirstOrDefault(c => c.Type == "requesting_system")?.Value;
        //
        // // Check if sub matches any of the valid claims
        // var validClaims = new[]
        // {
        //     requestingUser,
        //     requestingPatient,
        //     requestingSystem
        // }.Where(claim => !string.IsNullOrEmpty(claim));
        //
        // if (validClaims.All(claim => claim != subClaim))
        // {
        //     // sub must equal requesting_user, requesting_patient, or requesting_system
        //     issues.Add("The 'sub' claim must match one of: requesting_user, requesting_patient, or requesting_system");
        // }
        //
        // if (requestingUser != null && !requestingUser.StartsWith("https://fhir.nhs.uk/Id/sds-role-profile-id"))
        //     issues.Add("The 'requesting_user' claim must start with 'https://fhir.nhs.uk/Id/sds-role-profile-id'");
        //
        // if (requestingPatient != null && !requestingPatient.StartsWith("https://fhir.nhs.uk/Id/nhs-number"))
        //     issues.Add("The 'requesting_patient' claim must start with 'https://fhir.nhs.uk/Id/nhs-number'");
        //
        // if (requestingSystem != null && !requestingSystem.StartsWith("https://fhir.nhs.uk/Id/ods-organization-code"))
        //     issues.Add("The 'requesting_system' claim must start with 'https://fhir.nhs.uk/Id/ods-organization-code'");

        // Validate identifiers have the correct format (URI|value)
        foreach (var identifierType in new[]
                     { "sub", "requesting_system", "requesting_organization", "requesting_user", "requesting_patient" })
        {
            var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == identifierType)?.Value;
            if (!string.IsNullOrEmpty(claim) && !claim.Contains("|"))
                issues.Add($"Invalid format for '{identifierType}'. Expected format: [Naming system URI]|[Identifier]");
        }

        strings = issues.ToArray();
        return false;
    }

    /// <summary>
    /// Validates the "requested_scope" claims within the provided JWT token to ensure they align with the HTTP request method.
    /// </summary>
    /// <param name="jwtToken">The JWT token containing the claims to be validated.</param>
    /// <param name="requestMethod">The HTTP request method (e.g., GET, POST) being validated against the claims within the token.</param>
    /// <param name="issues">A list of string issues where any claim validation errors or mismatches will be recorded.</param>
    private static void ValidateRequestedScopeClaims(JwtSecurityToken jwtToken, string requestMethod,
        List<string> issues)
    {
        var requestingScope = jwtToken.Claims.FirstOrDefault(c => c.Type == "requested_scope")?.Value;
        if (WriteMethods.Contains(requestMethod))
        {
            if (!jwtToken.Claims.Any(c => c.Type == "requested_scope" && c.Value.Contains("write")))
                issues.Add("Missing 'requesting_scope' claim with 'write' scope");
        }
        else if (ReadMethods.Contains(requestMethod))
        {
            if (!jwtToken.Claims.Any(c => c.Type == "requested_scope" && c.Value.Contains("read")))
                issues.Add("Missing 'requesting_scope' claim with 'read' scope");
        }
    }

    /// <summary>
    /// Validates the 'requesting_practitioner' claim within a JWT to ensure it adheres to the expected structure
    /// and contains the necessary fields in the correct format according to NHS specifications.
    /// </summary>
    /// <param name="jwtToken">The JSON Web Token (JWT) containing the claims to be validated.</param>
    /// <param name="issues">A list to which any validation issues or errors will be added if found.</param>
    private static void ValidateRequestingPractitioner(JwtSecurityToken jwtToken, List<string> issues)
    {
        try
        {
            // Check if requesting_practitioner claim exists
            var requestingPractitionerClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "requesting_practitioner");
            if (requestingPractitionerClaim == null)
            {
                // Not mandatory, so we just return without adding an issue
                return;
            }

            // Try to parse the claim value as JSON
            var practitionerJson = requestingPractitionerClaim.Value;
            using var doc = System.Text.Json.JsonDocument.Parse(practitionerJson);
            var root = doc.RootElement;

            // Check resource type
            if (!root.TryGetProperty("resourceType", out var resourceType) ||
                resourceType.GetString() != "Practitioner")
            {
                issues.Add(
                    "Invalid 'requesting_practitioner' claim: Missing or invalid 'resourceType' - must be 'Practitioner'");
            }

            // Check ID field - required and must match the 'sub' claim
            if (!root.TryGetProperty("id", out var id) || string.IsNullOrEmpty(id.GetString()))
            {
                issues.Add("Invalid 'requesting_practitioner' claim: Missing or empty 'id'");
            }
            else
            {
                // ID should match the 'sub' claim
                var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (!string.IsNullOrEmpty(subClaim) && id.GetString() != subClaim)
                {
                    issues.Add("The 'requesting_practitioner.id' must match the 'sub' claim value");
                }
            }

            // Check identifier array
            if (!root.TryGetProperty("identifier", out var identifierArr) ||
                !identifierArr.ValueKind.HasFlag(System.Text.Json.JsonValueKind.Array))
            {
                issues.Add("Invalid 'requesting_practitioner' claim: Missing or invalid 'identifier' array");
            }
            else
            {
                bool hasSdsUserId = false;
                bool hasSdsRoleProfileId = false;
                bool hasLocalIdentifier = false;

                foreach (var identifier in identifierArr.EnumerateArray())
                {
                    if (identifier.TryGetProperty("system", out var system) &&
                        identifier.TryGetProperty("value", out var value) &&
                        !string.IsNullOrEmpty(system.GetString()) &&
                        !string.IsNullOrEmpty(value.GetString()))
                    {
                        string systemValue = system.GetString();

                        if (systemValue == "https://fhir.nhs.uk/Id/sds-user-id")
                        {
                            hasSdsUserId = true;
                        }
                        else if (systemValue == "https://fhir.nhs.uk/Id/sds-role-profile-id")
                        {
                            hasSdsRoleProfileId = true;
                        }
                        else if (systemValue.StartsWith("https://") && systemValue.Contains("/Id/"))
                        {
                            hasLocalIdentifier = true;
                        }
                    }
                }

                // Validate required identifiers
                if (!hasSdsUserId)
                {
                    issues.Add(
                        "Invalid 'requesting_practitioner' claim: Missing identifier with system 'https://fhir.nhs.uk/Id/sds-user-id'");
                }
            }

            // Check name field
            if (!root.TryGetProperty("name", out var nameArr) ||
                !nameArr.ValueKind.HasFlag(System.Text.Json.JsonValueKind.Array))
            {
                issues.Add("Invalid 'requesting_practitioner' claim: Missing or invalid 'name' array");
            }
            else
            {
                bool hasValidName = false;
                foreach (var name in nameArr.EnumerateArray())
                {
                    // Check family name
                    if (!name.TryGetProperty("family", out var family) || string.IsNullOrEmpty(family.GetString()))
                    {
                        continue;
                    }

                    // Check given name
                    if (!name.TryGetProperty("given", out var givenArr)
                        || !givenArr.ValueKind.HasFlag(System.Text.Json.JsonValueKind.Array)
                        || givenArr.GetArrayLength() <= 0)
                    {
                        continue;
                    }

                    bool hasNonEmptyGiven = false;
                    foreach (var given in givenArr.EnumerateArray())
                    {
                        if (!string.IsNullOrEmpty(given.GetString()))
                        {
                            hasNonEmptyGiven = true;
                            break;
                        }
                    }

                    if (hasNonEmptyGiven)
                    {
                        hasValidName = true;
                        break;
                    }
                }

                if (!hasValidName)
                {
                    issues.Add(
                        "Invalid 'requesting_practitioner' claim: 'name' must contain at least one entry with non-empty 'family' and 'given' elements");
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            issues.Add("Invalid 'requesting_practitioner' claim: Not a valid JSON object");
        }
        catch (Exception ex)
        {
            issues.Add($"Error validating 'requesting_practitioner' claim: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates the 'requesting_device' claim within a JWT to ensure it adheres to the expected structure
    /// and contains the necessary fields in the correct format.
    /// </summary>
    /// <param name="jwtToken">The JSON Web Token (JWT) containing the claims to be validated.</param>
    /// <param name="issues">A list to which any validation issues or errors will be added if found.</param>
    private static void ValidateRequestingDevice(JwtSecurityToken jwtToken, List<string> issues)
    {
        try
        {
            // Check if requesting_device claim exists
            var requestingDeviceClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "requesting_device");
            if (requestingDeviceClaim == null)
            {
                issues.Add("Missing mandatory claim: 'requesting_device'");
                return;
            }

            // Try to parse the claim value as JSON
            var deviceJson = requestingDeviceClaim.Value;
            using var doc = System.Text.Json.JsonDocument.Parse(deviceJson);
            var root = doc.RootElement;

            // Check resource type
            if (!root.TryGetProperty("resourceType", out var resourceType) || resourceType.GetString() != "Device")
            {
                issues.Add("Invalid 'requesting_device' claim: Missing or invalid 'resourceType' - must be 'Device'");
            }

            // Check identifier
            if (!root.TryGetProperty("identifier", out var identifierArr) ||
                !identifierArr.ValueKind.HasFlag(System.Text.Json.JsonValueKind.Array))
            {
                issues.Add("Invalid 'requesting_device' claim: Missing or invalid 'identifier' array");
            }
            else
            {
                bool hasValidIdentifier = false;
                foreach (var identifier in identifierArr.EnumerateArray())
                {
                    if (identifier.TryGetProperty("system", out var system) &&
                        identifier.TryGetProperty("value", out var value) &&
                        !string.IsNullOrEmpty(system.GetString()) &&
                        !string.IsNullOrEmpty(value.GetString()))
                    {
                        hasValidIdentifier = true;
                        break;
                    }
                }

                if (!hasValidIdentifier)
                {
                    issues.Add(
                        "Invalid 'requesting_device' claim: Identifier must contain at least one entry with non-empty 'system' and 'value' elements");
                }
            }

            // Check model
            if (!root.TryGetProperty("model", out var model) || string.IsNullOrEmpty(model.GetString()))
            {
                issues.Add("Invalid 'requesting_device' claim: Missing or empty 'model'");
            }

            // Check version
            if (!root.TryGetProperty("version", out var version) || string.IsNullOrEmpty(version.GetString()))
            {
                issues.Add("Invalid 'requesting_device' claim: Missing or empty 'version'");
            }
        }
        catch (System.Text.Json.JsonException)
        {
            issues.Add("Invalid 'requesting_device' claim: Not a valid JSON object");
        }
        catch (Exception ex)
        {
            issues.Add($"Error validating 'requesting_device' claim: {ex.Message}");
        }
    }

    private static void ValidateActiveToken(JwtSecurityToken jwtToken, List<string> issues)
    {
        if (jwtToken.ValidTo == default)
            issues.Add("Missing mandatory claim: 'exp' (Expiration Time)");

        if (jwtToken.ValidTo != default && jwtToken.ValidTo > DateTime.Now.AddMinutes(5))
        {
            issues.Add("exp claim is too far in the future");
        }

        if (jwtToken.ValidTo != default && jwtToken.ValidTo < DateTime.Now.AddMinutes(-5))
        {
            issues.Add("exp has expired");
        }

        if (jwtToken.ValidFrom == default)
            issues.Add("Missing mandatory claim: 'iat' (Issued At)");
    }
}