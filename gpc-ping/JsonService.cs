using System.Text.Json;

namespace gpc_ping;

public static class JsonService
{
    public static T? DeserializeClaim<T>(string stream)
    {
        try
        {
            var deserializedObject = JsonSerializer.Deserialize<T>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return deserializedObject;
        }
        catch (JsonException ex)
        {
            return default(T);
        }
    }
}