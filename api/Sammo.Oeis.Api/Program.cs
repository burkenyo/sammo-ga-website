using Sammo.Oeis;
using Sammo.Oeis.Api;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration.Get<Config>() ?? new Config();
WebApplication app;

// Add dependency-injection services.
try
{
    var services = builder.Services;
    
    if (config.NoAzure)
    {
        services.AddOeisDozenalExpansionFileStore(config.FileStore);
    }
    else
    {
        var cred = Startup.GetAzureCredential(config.Azure);

        builder.Configuration.AddKeyVaultStoredConfiguration(config.Azure, cred);
        
        // Rebind config now that I’ve loaded the rest
        builder.Configuration.Bind(config);

        services.AddOeisDozenalExpansionAzureBlobStore(config.Azure?.Blobs, cred);
    }

    services.AddHttpClient<IOeisDecimalExpansionDownloader, OeisDecimalExpansionDownloader>();

    // because downloader captures an HttpClient-backed downloader, it should not be singleton
    services.AddScoped<IOeisDozenalExpansionService, OeisDozenalExpansionService>();

    services.AddWebApi<ExpansionsApi>();

    services.AddEndpointsApiExplorer();
    services.AddThisAssemblySwaggerGen();

    services.ConfigureJsonOptions();

    app = builder.Build();
}
catch (Exception ex)
{
#pragma warning disable ASP0000 // Do not build the service provider in application code
    var services = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
    
    Startup.LogError(services, builder.Environment, ex);
    
    return 1;
}

// Configure the request pipeline and run the app.
try
{
    app.UseSwagger();
    app.UseThisAssemblySwaggerUi();

    app.MapRootToSwagger();
    app.Map<ExpansionsApi>();

    Startup.CheckDebugAllowed(app.Environment, config);

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Startup.LogError(app.Services, app.Environment, ex);

    return 1;
}