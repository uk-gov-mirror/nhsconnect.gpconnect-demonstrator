# Access Tokens and Audit (JWT) | CareConnect
Overview of how authorisation information is used and passed in APIs for audit and provenance.

Use of Bearer Tokens
--------------------

An output of [authorising access](security_authorisation.html) to an API is the provision of a JSON Web Token. This MUST be passed in the API calls to ensure the systems being called are able to verify that the user has been authorised to see the resources requested. This JWT is also used for audit purposes, so the API implementation (and the SSP in the case of a call brokered through that service) can record the user context in it’s audit trail.

In order to achieve this, the Consumer MUST include Access token in the HTTP authorisation header as an oAuth Bearer Token (as outlined in [RFC 6749](https://tools.ietf.org/html/rfc6749)) in the form of a JSON Web Token (JWT) as defined in [RFC 7519](https://tools.ietf.org/html/rfc7519).

An example such an HTTP header is given below:

```
     Authorization: Bearer jwt_token_string

```


API Provider systems SHALL respond to oAuth Bearer Token errors in line with [RFC 6750 - section 3.1](https://tools.ietf.org/html/rfc6750#section-3.1).

It is highly recommended that standard libraries are used for creating the JWT as constructing and encoding the token manually may lead to issues with parsing the token. A good source of information about JWT and libraries to use can be found on the [JWT.io site](https://jwt.io/)

The new national authentication service is not yet in place, so it is not currently possible to request an Access token from that service. In the interim, Consumer systems are expected to either use a local service to generate tokens, or generate a new JWT for each API request themselves. A consumer generated JSON Web Token (JWT) SHALL consisting of three parts seperated by dots `.`, which are:

*   Header
*   Payload
*   Signature

Where Consumer systems are generating the JWT locally, they SHALL generate an Unsecured JSON Web Token (JWT) using the ‘none’ algorithm parameter in the header to indicate that no digital signature or MAC has been performed (please refer to section 6 of [RFC 7519](https://tools.ietf.org/html/rfc7519) for details).

```
{
  "alg": "none",
  "typ": "JWT"
}

```


### Payload

Consumers systems SHALL generate a JWT Payload conforming to the requirements set out in the [JWT Payload](#jwt-payload) section of this page.

### Signature

Consumer systems SHALL generate an empty signature.

### Complete JWT

The final output is three Base64url encoded strings separated by dots (note - there is some canonicalisation done to the JSON before it is Base64url encoded, which the JWT code libraries will do for you).

```
eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJpc3MiOiJodHRwczovL2Nhcy5uaHMudWsiLCJzdWIiOiJodHRwczovL2ZoaXIubmhzLnVrL0lkL3Nkcy1yb2xlLXByb2ZpbGUtaWR8Mzg3NDI5Nzg1MzA5Mjc1IiwiYXVkIjoiaHR0cHM6Ly9jbGluaWNhbHMuc3BpbmVzZXJ2aWNlcy5uaHMudWsiLCJleHAiOjE0Njk0MzY5ODcsImlhdCI6MTQ2OTQzNjY4NywicmVhc29uX2Zvcl9yZXF1ZXN0IjoiZGlyZWN0Y2FyZSIsInNjb3BlIjoicGF0aWVudC8qLnJlYWQiLCJyZXF1ZXN0aW5nX3N5c3RlbSI6Imh0dHBzOi8vZmhpci5uaHMudWsvSWQvYWNjcmVkaXRlZC1zeXN0ZW18MjAwMDAwMDAwMjA1IiwicmVxdWVzdGluZ191c2VyIjoiaHR0cHM6Ly9maGlyLm5ocy51ay9JZC9zZHMtcm9sZS1wcm9maWxlLWlkfDQzODcyOTM4NzQ5MjgifQ.

```


NOTE: The final section (the signature) is empty, so the JWT will end with a trailing `.` (this must not be omitted, otherwise it would be an invalid token).

JWT Payload
-----------

The Payload section of the JWT shall be populated as follows:


| Claim | Mandatory | Description | Fixed Value | Dynamic Value |
| --- | --- | --- | --- | --- |
| iss | Y | Requesting systems issuer URI | No | Yes |
| sub | Y | ID for the user on whose behalf this request is being made. Will match either the requesting_user, requesting_patient (or requesting_system for unattended APIs) | No | Yes |
| aud | Y | API endpoint URL | No | Yes |
| exp | Y | Expiration time integer after which this authorization MUST be considered invalid. | No | (now + 5 minutes) UTC time in seconds |
| iat | Y | The UTC time the JWT was created by the requesting system | No | now UTC time in seconds |
| reason_for_request | Y | Purpose for which access is being requested | directcare, secondaryuses or patientaccess | No |
| scope | Y | Data being requested | patient/_.[read|write] OR organization/_.[read|write] | No |
| requesting_system | Y | Identifier for the system or device making the request | No | System or Device Identifier |
| requesting_organization | N | Organisation making the request | No | Organisation Identifier |
| requesting_user | N | Health or Social Care professional making the request | No | User Identifier |
| requesting_patient | N | Citizen making the request | No | NHS Number |


### JWT Payload Example

This is an example of how the JWT might look for an authorized practitioner making a request to view a patient record:

```
{
	"iss": "https://cas.nhs.uk",
	"sub": "https://fhir.nhs.uk/Id/sds-role-profile-id|[SDSRoleProfileID]",
	"aud": "https://provider.thirdparty.nhs.uk/GP0001/STU3/1",
	"exp": 1469436987,
	"iat": 1469436687,
	"reason_for_request": "directcare",
	"scope": "patient/*.read",
	"requesting_system": "https://fhir.nhs.uk/Id/accredited-system|[ASID]",
	"requesting_organization": "https://fhir.nhs.uk/Id/ods-organization-code|[ODSCode]",
	"requesting_user": "https://fhir.nhs.uk/Id/sds-role-profile-id|[SDSRoleProfileID]"
}

```


Where the Practitioner has both a local system identifier but no Spine identifier, then the a local identifier MAY be used with a suitable URI to identify the system/organisation that assigned the ID.

### Identifier Prefixes

All identifiers used in the JWT follow the same approach used for other FHIR identifiers - i.e. they have a prefix to identify the naming system the identifier comes from. This allows different naming systems to be used without any risk of confusion if the same identifier exists in different naming systems. The identifier should be a string, with the naming systen URI followed by a pipe symbol (|), followed by the identifier value - i.e.:

```
[Naming system URI]|[Identifier]

```


For example:

```
https://fhir.nhs.uk/Id/ods-organization-code|X09

```


Naming systems appropriate for specific types of identifiers are listed in the population guidance below.

### Population of JWT attributes

Common attributes are as defined in [rfc7519](https://tools.ietf.org/html/rfc7519#section-4.1.3) - but the below table provides specific guidance on the values of specific attributes:



| Attribute Name | Attribute Value Guidance |
| --- | --- |
| iss | (Issuer) For tokens issued by the national authentication solution, this should be the URL of that national service. For tokens issued by a different issuer, this will hold the URL for the issuer. |
| sub | (Subject) An identifier for the person or system that has been authorised for access. In most cases this should be an identifier for a member of staff, identified by a Spine URP ID (see RBAC) for details. In cases where the API is being accessed by a citizen, this should be the NHS Number of that individual. In unattended API calls where authorisation has been granted to a system rather than a user, the system identifier should be used here (i.e. the Spine ASID) |
| aud | (Audience) The URI for the API Endpoint. This should be the fully qualified endpoint address returned to the Consumer by the SDS endpoint lookup service as the value of nhsMhsEndPoint. |
| reason_for_request | This identified the purpose for which the request is being made. This is currently limited to one of the following values: directcare, secondaryuses or patientaccess. |
| scope | This is a space-separated list of the scopes authorised for this user. Individual APIs will check this list to establish whether the authorisation grants access to the data being request by the client, and will reject the call if the appropriate scopes have not been granted in the token. |
| requesting_system | This is an identifier for the deployed client system that has been authorised to make API calls. In the case of Spine-enabled clients (or those using the SSP to broker API calls), this should be a Spine Accredited System ID (ASID)The naming system prefix for the ASID should start with "[https://fhir.nhs.uk/Id/accredited-system](https://fhir.nhs.uk/Id/accredited-system)" |
| requesting_organization | This is the ODS code of the care organisation from where the request originates.The naming system prefix for the ODS code should start with "[https://fhir.nhs.uk/Id/ods-organization-code](https://fhir.nhs.uk/Id/ods-organization-code)" This is likely to be mandatory for APIs used by professionals - the specific API documentation for each API will clarify whether this must be used. |
| requesting_practitioner | If this authorisation relates to a member of staff, this attribute will hold the Spine URP ID (see RBAC) for details. In this case, the value of this attribute will match the “sub” attribute value.The naming system prefix for the URP ID code should start with "[https://fhir.nhs.uk/Id/sds-role-profile-id](https://fhir.nhs.uk/Id/sds-role-profile-id)" Where national authorisation is not being used (e.g. where Spine is brokering a call to a local system that has authorised the API access directly), and a national SDS role profile ID can’t be used, a local user identifier may be used instead.The naming system prefix for a local identifier should be a locally-defined URI specific to the system/service that managed the user identities - e.g. [https://my-care-service/Id/user-id](https://my-care-service/Id/user-id) Where the user has both a local system ‘role’ as well as a nationally-recognised role, then the latter SHALL be provided. Default usernames (e.g. referring to systems or groups) SHALL NOT be used in this field.This is likely to be mandatory for APIs used by professionals - the specific API documentation for each API will clarify whether this must be used. |
| requesting_patient | If this authorisation relates to a citizen, this attribute will hold the NHS Number of the citizenThe naming system prefix for the NHS Number should start with "[http://fhir.nhs.net/Id/nhs-number](http://fhir.nhs.net/Id/nhs-number)" This is likely to be mandatory for APIs used by patienrs/citizens - the specific API documentation for each API will clarify whether this must be used. |


Example JWT for an authorised professional
------------------------------------------

The below is an example of how the JWT might look for a health or social care professional accessing a Spine Clinicals API to retrieve data about a patient:

```
{
	"iss": "https://cas.nhs.uk",
	"sub": "https://fhir.nhs.uk/Id/sds-role-profile-id|387429785309275",
	"aud": "https://clinicals.spineservices.nhs.uk",
	"exp": 1469436987,
	"iat": 1469436687,
	"reason_for_request": "directcare",
	"scope": "patient/*.read",
	"requesting_system": "https://fhir.nhs.uk/Id/accredited-system|200000000205",
	"requesting_user": "https://fhir.nhs.uk/Id/sds-role-profile-id|4387293874928"
}

```


Example JWT for an “unattended” API call (i.e. with no user identity included)
------------------------------------------------------------------------------

The below is an example of how the JWT might look for an API call between two systems without a specific user context:

```
{
	"iss": "https://cas.nhs.uk",
	"sub": "https://fhir.nhs.uk/Id/accredited-system|200000000205",
	"aud": "https://clinicals.spineservices.nhs.uk",
	"exp": 1469436987,
	"iat": 1469436687,
	"reason_for_request": "directcare",
	"scope": "patient/*.read",
	"requesting_system": "https://fhir.nhs.uk/Id/accredited-system|200000000205",
}

```


Example JWT for an authorised citizen
-------------------------------------

The below is an example of how the JWT might look for a citizen accessing an API to retrieve and update information about their consent preferences:

```
{
	"iss": "https://citizen-id.nhs.uk",
	"sub": "http://fhir.nhs.net/Id/nhs-number|6101231234",
	"aud": "https://clinicals.spineservices.nhs.uk",
	"exp": 1469436987,
	"iat": 1469436687,
	"reason_for_request": "patientaccess",
	"scope": "patient/consent.read patient/consent.write",
	"requesting_system": "https://fhir.nhs.uk/Id/accredited-system|200000000205",
	"requesting_patient": "http://fhir.nhs.net/Id/nhs-number|6101231234"
}

```


In the above example:

*   The access token was issued by the national Citizen identity service, so that service’s URL is in the issuer (iss) field (this service does not exist yet, so the URL is an example only)
*   The patient has been identified as having the NHS number **6101231234**
*   The subject of the authorisation decision was the patient, so the **sub** value is the same as the **requesting\_patient** value
*   The service that provides the APIs being authorised is the Spine clinicals service, so the audience (aud) URL is for that service
*   Issued and Expiry times are populated by the authorisation server to limit the lifetime of the access token
*   The information is being requested by the patient, so the reason for the request is **patientaccess**
*   The application developer has reviewed the API specification and identified that the scopes required for this data are: **patient/consent.read patient/consent.write**
*   The patient is using an application that has been reviewed and approved for access to Spine services, and given a system ID of **200000000205**
