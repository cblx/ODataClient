namespace OData.Client;

internal class SelectAndExpandResult {
    public string Query { get; set; } = default!;
    public bool HasFormattedValues { get; set; }
}
