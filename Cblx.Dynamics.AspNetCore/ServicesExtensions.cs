using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OData.Client;
using OData.Client.Abstractions;
namespace Cblx.Dynamics.AspNetCore;

public static class ServicesExtensions
{
    public static void AddDynamics(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<DynamicsOptions>? setup = null)
    {
        DynamicsOptions? options = new();
        setup?.DynamicInvoke(options);
        services.AddSingleton<ODataClientOptions>(options);
        services.AddSingleton<IDynamicsAuthenticator, DynamicsAuthenticator>();
        services
            .AddOptions<DynamicsConfig>()
            .Configure(o => configuration.GetSection("Dynamics").Bind(o));

        services
            .AddHttpClient(
                nameof(IODataClient),
                // Sets a fake baseaddress to deceive HttpClient initial validation.
                // The baseAddress will be modified by the DynamicsAuthorizationMessageHandler.
                httpClient => httpClient.BaseAddress = new UriBuilder("https", "d").Uri
            )
            .AddHttpMessageHandler<DynamicsAuthorizationMessageHandler>();

        //services.AddHttpClient(nameof(IODataClient), (sp, client) =>
        //{
        //    var dynamicsAuthenticator = sp.GetService<DynamicsAuthenticator>()!;
        //     var onCreateClientContext = new OnCreateClientContext();
        //    options.OnCreateClient?.DynamicInvoke(sp, onCreateClientContext);
        //    dynamicsAuthenticator.AuthenticateHttpClient(client, onCreateClientContext.OverrideResourceUrl).GetAwaiter().GetResult();
        //});
        services.AddScoped<IODataClient, ODataClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IODataClient));
            var oDataClient = new ODataClient(httpClient, sp.GetService<ODataClientOptions>());
            return oDataClient;
        });
    }
}