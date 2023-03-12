namespace Cblx.Dynamics;
/// <summary>
/// Sets the rerential constraint for a nav property
/// This is not necessary when using Dynamics metadata:
/// .AddDynamics(options => options.DownloadMetadataAndConfigure = true)
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReferentialConstraintAttribute : Attribute
{
    public string Property { get; }
    public string RawPropertyName { get; }
    public ReferentialConstraintAttribute(string property)
    {
        Property = property;
        RawPropertyName = TrimLookupName(property);
    }

    static string TrimLookupName(string lookupName)
    {
        if (lookupName.StartsWith("_"))
        {
            return lookupName[1..^6];
        }
        return lookupName;
    }
}