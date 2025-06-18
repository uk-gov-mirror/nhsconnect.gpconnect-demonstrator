using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace gpc_ping;

public interface IValidationCommonValidation
{
    (bool IsValid, string[] Messages, T? Practitioner)
        DeserializeAndValidateCommonRequestingPractitionerProperties<T>(Claim? requestingPractitionerClaim)
        where T : RequestingPractitioner;

    (bool IsValid, string[] Messages) ValidateRequestingPractitionerIdentifier(
        RequestingPractitioner requestingPractitioner, int requiredLength);

    (bool IsValid, string Message) ValidateRequestingDeviceCommon(RequestingDevice requestingDevice);

    (bool IsValid, string[] Messages, T? DeserializedClaim) ValidateRequestingOrganizationCommon<T>(
        JwtSecurityToken token)
        where T : RequestingOrganization;
}