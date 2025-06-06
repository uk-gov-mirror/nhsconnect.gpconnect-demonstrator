namespace gpc_ping;

public class RequestingPractitioner
{
    public string Id { get; set; }
    public Identifier[] Identifier { get; set; }
    public Name Name { get; set; }
}

public class Identifier
{
    public string System { get; set; }
    public string Value { get; set; }
}

public class Name
{
    public string[] Family { get; set; }
    public string[] Given { get; set; }
    public string[] Prefix { get; set; }
}