namespace Cblx.OData.Client.Abstractions.Ids;
public interface IHasStronglyTypedId<TId> where TId : Id
{
    TId Id { get; }
}