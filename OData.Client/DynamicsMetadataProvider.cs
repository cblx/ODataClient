using OData.Client;

namespace Cblx.Dynamics;

/// <summary>
/// Infer types from a Dynamics Schema, and fallback to C# annotations and typings
/// </summary>
public class DynamicsMetadataProvider : DynamicsCodeMetadataProvider
{
    //private static IHttpClientFactory _httpClientFactory;
    //private static readonly Lazy<ResourceMetadata> _metadata = new Lazy<ResourceMetadata>(async () =>
    //{
    //    await Task.CompletedTask;
    //    return new ResourceMetadata();
    //});

    public DynamicsMetadataProvider(IHttpClientFactory httpClientFactory, DynamicsOptions options)
    {
        //_httpClientFactory = httpClientFactory;
        if (options.DownloadMetadataAndConfigure)
        {
            httpClientFactory.CreateClient(options.HttpClientName!);
        }
    }

    //public string GetColumnName<TEntity>(string propertyName) => typeof(TEntity).GetProperties().FirstOrDefault(p => p.Name == propertyName || p.Get)

    public override bool IsEdmDate<TEntity>(string columnName) where TEntity : class
    {
        return base.IsEdmDate<TEntity>(columnName);
    }
}