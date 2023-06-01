using OData.Client.Abstractions;

namespace Cblx.OData.Client.Tests.Repositories.Next.Simple;
public class Repository : ODataRepository<DomainEntity, TbEntity>
{
    public Repository(IODataClient oDataClient) : base(oDataClient)
    {
    }

    public async Task<DomainEntity> GetAsync(Guid id) => await GetAsync<DomainEntity>(id);
}