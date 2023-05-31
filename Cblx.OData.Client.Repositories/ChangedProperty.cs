using System.Reflection;

namespace Cblx.OData.Client;

internal class ChangedProperty
{
    public string? FieldLogicalName { get; set; }
    public string? NavigationLogicalName { get; set; }
    //public PropertyInfo PropertyInfo { get; set; } = default!;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}
