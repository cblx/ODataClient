using OData.Client;

namespace Cblx.Dynamics;

public class ResourceMetadataProvider
{
    //private static IHttpClientFactory _httpClientFactory;
    //private static readonly Lazy<ResourceMetadata> _metadata = new Lazy<ResourceMetadata>(async () =>
    //{
    //    await Task.CompletedTask;
    //    return new ResourceMetadata();
    //});

    public ResourceMetadataProvider(IHttpClientFactory httpClientFactory, DynamicsOptions options)
    {
        //_httpClientFactory = httpClientFactory;
        if (options.DownloadMetadataAndConfigure)
        {
            httpClientFactory.CreateClient(options.HttpClientName!);
        }
    }
}