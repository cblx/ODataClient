namespace Cblx.OData.Client;

public interface IChangeTracker
{
    void Add(object o);
    TEntity? AttachOrGetCurrent<TEntity>(TEntity? e);
    IEnumerable<TEntity> AttachOrGetCurrentRange<TEntity>(IEnumerable<TEntity?> items);
    internal Change? GetChange(Guid id);
    internal IEnumerable<Change> GetChanges();
    void Remove(object o);
    internal void AcceptChange(Change change);
}