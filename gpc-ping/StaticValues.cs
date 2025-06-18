namespace gpc_ping;

public static class StaticValues
{
    public static string[] BaseScopes =
    [
        "patient/*.read", "patient/*.write", "organization/*.read", "organisation/*.write"
    ];

    public static string SupportedApiVersions = "v0.7.4', 'v1.2.7', 'v1.5.0', 'v1.6.0'";
    public static string V074AudienceUrl = "https://authorize.fhir.nhs.net/token";
}