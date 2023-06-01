namespace Cblx.Dynamics;

internal class DynamicsEntityProperty
{
    public string LogicalName { get; internal set; } = string.Empty;
    /// <summary>
    /// If this is a Foreign Key, the related navigation name is stored here
    /// </summary>
    public string? RelatedLogicalNavigationName { get; internal set; }
    /// <summary>
    /// If this is a Navigation Property, the related lookup name (_field_value) is stored here
    /// </summary>
    public string? RelatedLogicalLookupName { get; internal set; }
    public string? RelatedLogicalLookupRawName
    {
        get
        {
            if(RelatedLogicalLookupName == null) { return null; }
            if (RelatedLogicalLookupName.StartsWith("_"))
            {
                return RelatedLogicalLookupName[1..^6];
            }
            return RelatedLogicalLookupName;
        }
    }
}