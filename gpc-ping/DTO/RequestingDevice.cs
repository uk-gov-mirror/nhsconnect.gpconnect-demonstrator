namespace gpc_ping;

public class RequestingDevice
{
    public string ResourceType { get; set; }
    public Identifier[] Identifier { get; set; }
    public string? Model { get; set; }
    public string? Version { get; set; }
}