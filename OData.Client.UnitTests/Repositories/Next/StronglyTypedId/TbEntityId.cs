#nullable enable
using Cblx.OData.Client.Abstractions.Ids;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.Repositories.Next.StronglyTypedId;

[TypeConverter(typeof(IdTypeConverter<TbEntityId>))]
[JsonConverter(typeof(IdConverterFactory))]
public record TbEntityId(Guid guid) : Id(guid)
{
    public TbEntityId(string guidString) : this(new Guid(guidString)) { { } }
    public static implicit operator Guid(TbEntityId? id) => id?.Guid ?? Guid.Empty;
    public static implicit operator Guid?(TbEntityId? id) => id?.Guid;
    public static explicit operator TbEntityId(Guid guid) => new TbEntityId(guid);
    public static TbEntityId Empty { get; } = new TbEntityId(Guid.Empty);
    public static TbEntityId NewId() => new TbEntityId(Guid.NewGuid());
    public override string ToString() => Guid.ToString();
}