namespace gpc_ping;

public class V074RequestingPractitioner : RequestingPractitioner
{
    public string ResourceType { get; set; }
    public PractitionerRole[] PractitionerRole { get; set; } = [];
}

public class PractitionerRole
{
    public Role? Role { get; set; }
}

public class Role
{
    public Coding[] Coding { get; set; }
}

public class Coding
{
    public string System { get; set; }
    public string Code { get; set; }
}