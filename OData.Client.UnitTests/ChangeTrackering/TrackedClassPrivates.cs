using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests;
#pragma warning disable S3453 // Classes should not have only "private" constructors
public partial class TrackedClassPrivates
#pragma warning restore S3453 // Classes should not have only "private" constructors
{
    private TrackedClassPrivates()
    {

    }

    [JsonPropertyName(nameof(Id))]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [JsonPropertyName(nameof(Name))]
    public string Name { get; private set; }

    [JsonPropertyName(nameof(Description))]
    public string Description { get; private set; }

    [ReadOnly(true)]
    public string NotTracked { get; private set; }

    public void ChangeNotTracked(string val) => NotTracked = val;

    public void ChangeName(string name) => Name = name;
}
