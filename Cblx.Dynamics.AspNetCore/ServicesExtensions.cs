using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OData.Client;
using OData.Client.Abstractions;
namespace Cblx.Dynamics.AspNetCore;

public static class ServicesExtensions
{
    public static void AddDynamics(this IServiceCollection services, Action<DynamicsOptions>? setup = null) => AddDynamics(services, (Delegate?)setup);
    private static void AddDynamics(this IServiceCollection services, Delegate? setup = null)
    {
        // Options configuration
        services.AddSingleton(sp =>
        {
            var options = new DynamicsOptions();
            if (setup != null)
            {
                switch (setup.Method.GetParameters().Length)
                {
                    case 1:
                        setup.DynamicInvoke(options);
                        break;
                    case 2:
                        setup.DynamicInvoke(sp, options);
                        break;
                }
                if (setup.Method.GetParameters().Length == 1)
                {
                    setup?.DynamicInvoke(options);
                }
            }
            return options;
        });
        
        services.AddSingleton<IDynamicsAuthenticator, DynamicsAuthenticator>();
        // Default Config, binding from "Dynamics" section
        services
            .AddOptions<DynamicsConfig>()
            .Configure<IConfiguration>((dynamicsConfig, configuration) => configuration.GetSection("Dynamics").Bind(dynamicsConfig));
        services.AddScoped(sp => sp.GetRequiredService<IOptions<DynamicsConfig>>().Value);

        // Default HttpClient named IODataClient, that uses the default config for BaseAddress resolution
        services
            .AddHttpClient(nameof(IODataClient))
            .AddHttpMessageHandler<DynamicsAuthorizationMessageHandler>()
            .ConfigureHttpClient((sp, httpClient) =>
            {
                var dynamicsConfig = sp.GetRequiredService<IOptions<DynamicsConfig>>().Value;
                httpClient.BaseAddress = DynamicsBaseAddress.FromResourceUrl(dynamicsConfig.ResourceUrl);
            });
        services.AddScoped<DynamicsAuthorizationMessageHandler>();

        services.AddScoped<IODataClient, ODataClient>(sp =>
        {
            var options = sp.GetRequiredService<DynamicsOptions>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(options.HttpClientName!);
            var oDataClient = new ODataClient(httpClient, options);
            return oDataClient;
        });
    }
}