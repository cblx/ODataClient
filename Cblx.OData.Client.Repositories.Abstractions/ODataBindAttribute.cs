using System;

namespace Cblx.OData.Client;
/// <summary>
/// Sets the nav prop for binding to this FK
/// This is not necessary when using Dynamics metadata:
/// .AddDynamics(options => options.DownloadMetadataAndConfigure = true)
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ODataBindAttribute : Attribute {
    public string Name { get; private set; }
    public ODataBindAttribute(string name)
    {
        Name = name;
    }
}
