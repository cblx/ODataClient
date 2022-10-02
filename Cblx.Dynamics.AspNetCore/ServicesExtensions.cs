using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OData.Client;
using OData.Client.Abstractions;
using System.Net.Http.Headers;

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


        //DynamicsOptionsBuilder? optionsBuilder = new();
        //setup?.DynamicInvoke(optionsBuilder);
        //services.AddSingleton<ODataClientOptions>(options);





        //services.AddHttpClient(nameof(IODataClient), (sp, client) =>
        //{
        //    var dynamicsAuthenticator = sp.GetService<DynamicsAuthenticator>()!;
        //     var onCreateClientContext = new OnCreateClientContext();
        //    options.OnCreateClient?.DynamicInvoke(sp, onCreateClientContext);
        //    dynamicsAuthenticator.AuthenticateHttpClient(client, onCreateClientContext.OverrideResourceUrl).GetAwaiter().GetResult();
        //});

        services.AddScoped<IODataClient, ODataClient>(sp =>
        {
            var options = sp.GetRequiredService<DynamicsOptions>();
            var httpClient = options.HttpClient ?? sp.GetRequiredService<IHttpClientFactory>().CreateClient(options.HttpClientName!);
            var oDataClient = new ODataClient(httpClient, options);
            return oDataClient;
        });
    }


    //public static void AddDynamics(
    //    this IServiceCollection services, 
    //    IConfiguration configuration,
    //    Action<DynamicsOptions>? setup = null)
    //{
    //    DynamicsOptions? options = new();
    //    setup?.DynamicInvoke(options);
    //    services.AddSingleton<ODataClientOptions>(options);
    //    services.AddSingleton<IDynamicsAuthenticator, DynamicsAuthenticator>();
    //    services
    //        .AddOptions<DynamicsConfig>()
    //        .Configure(o => configuration.GetSection("Dynamics").Bind(o));

    //    services.AddScoped<DynamicsAuthorizationMessageHandler>();
    //    services
    //        .AddHttpClient(nameof(IODataClient))
    //        .AddHttpMessageHandler<DynamicsAuthorizationMessageHandler>()
    //        .ConfigureHttpClient((sp,httpClient) =>
    //        {
    //            var dynamicsConfig = sp.GetRequiredService<IOptions<DynamicsConfig>>().Value;
    //            httpClient.BaseAddress = new Uri(new Uri(dynamicsConfig.ResourceUrl), "api/data/v9.0");
    //        });

    //    //services.AddHttpClient(nameof(IODataClient), (sp, client) =>
    //    //{
    //    //    var dynamicsAuthenticator = sp.GetService<DynamicsAuthenticator>()!;
    //    //     var onCreateClientContext = new OnCreateClientContext();
    //    //    options.OnCreateClient?.DynamicInvoke(sp, onCreateClientContext);
    //    //    dynamicsAuthenticator.AuthenticateHttpClient(client, onCreateClientContext.OverrideResourceUrl).GetAwaiter().GetResult();
    //    //});
    //    services.AddScoped<IODataClient, ODataClient>(sp =>
    //    {
    //        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IODataClient));
    //        var oDataClient = new ODataClient(httpClient, sp.GetService<ODataClientOptions>());
    //        return oDataClient;
    //    });
    //}
}