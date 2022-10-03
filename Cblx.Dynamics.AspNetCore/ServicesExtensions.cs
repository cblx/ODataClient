using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OData.Client;
using OData.Client.Abstractions;
namespace Cblx.Dynamics.AspNetCore;

public static class ServicesExtensions
{
    public static void AddDynamics(this IServiceCollection services, Action<DynamicsOptionsBuilder>? setup = null) => AddDynamics(services, (Delegate?)setup);
    public static void AddDynamics(this IServiceCollection services, Action<IServiceProvider, DynamicsOptionsBuilder>? setup) => AddDynamics(services, (Delegate?) setup);
    private static void AddDynamics(this IServiceCollection services, Delegate? setup = null)
    {
        // Options configuration
        services.AddScoped(sp =>
        {
            var optionsBuilder = new DynamicsOptionsBuilder();
            if (setup != null)
            {
                switch (setup.Method.GetParameters().Length)
                {
                    case 1:
                        setup.DynamicInvoke(optionsBuilder);
                        break;
                    case 2:
                        setup.DynamicInvoke(sp, optionsBuilder);
                        break;
                }
                if (setup.Method.GetParameters().Length == 1)
                {
                    setup?.DynamicInvoke(optionsBuilder);
                }
            }
            return optionsBuilder.Options;
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
            var httpClient = options.HttpClient ?? sp.GetRequiredService<IHttpClientFactory>().CreateClient(options.HttpClientName!);
            var oDataClient = new ODataClient(httpClient, options);
            return oDataClient;
        });
    }
}