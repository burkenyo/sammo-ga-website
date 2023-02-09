using Sammo.Oeis;
using Sammo.Oeis.Api;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration.Get<Config>() ?? new();
WebApplication app;

DirectoryInfo? dataDir = null;

// Add dependency-injection services.
try
{
    var services = builder.Services;

    // because downloader captures an HttpClient-backed downloader, it should not be singleton
    if (config.NoAzure)
    {
        services.AddOeisDozenalExpansionFileStore(config.FileStore, out dataDir);

        services.AddHttpContextAccessor();

        services.AddLocalTestingOeisDozenalExpansionService(dataDir);
    }
    else
    {
        var cred = Startup.GetAzureCredential(config.Azure);

        builder.Configuration.AddKeyVaultStoredConfiguration(config.Azure, cred);

        // Rebind config now that I’ve loaded the rest
        builder.Configuration.Bind(config);

        services.AddOeisDozenalExpansionAzureBlobStore(config.Azure.Blobs, cred);

        services.AddScoped<IOeisDozenalExpansionService, OeisDozenalExpansionService>();
    }

    services.AddHttpClient<IOeisDecimalExpansionDownloader, OeisDecimalExpansionDownloader>();

    services.AddWebApi<ExpansionsApi>();

    services.AddEndpointsApiExplorer();
    services.AddThisAssemblySwaggerGen();

    if (config.UseCors)
    {
        services.AddCors(config.Cors);
    }

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

    if (config.UseCors)
    {
        app.UseCors();
    }

    if (config.NoAzure)
    {
        // It's important to call this after UseCors!
        app.UseDataDirStaticFiles(dataDir!);
    }

    app.MapRootToSwagger();
    app.Map<ExpansionsApi>();
    app.MapGitInfo();

    Startup.CheckDebugAllowed(app.Environment, config);

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Startup.LogError(app.Services, app.Environment, ex);

    return 1;
}
