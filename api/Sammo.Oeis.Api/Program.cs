using System.Diagnostics;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.Json;
using Sammo.Oeis;
using Sammo.Oeis.Api;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

CheckDebugAllowed(config);

if (config.GetValue<bool>("UseFileStore"))
{
    builder.Services.AddSingleton<IOeisDozenalExpansionStore>(provider =>
    {
        var fileStoreConfig = config.GetRequiredSection("FileStore");
        var dataDir = new DirectoryInfo(GetRequiredConfigValue(fileStoreConfig, "DataDirectory"));

        if (!dataDir.Exists)
        {
            dataDir.Create();
        }

        return new OeisDozenalExpansionFileStore(dataDir);
    });
}
else
{
    builder.Services.AddSingleton<IOeisDozenalExpansionStore>(provider =>
    {
        var blobConfig = config.GetRequiredSection("Azure:Blobs");

        var accountName = GetRequiredConfigValue(blobConfig, "AccountName");
        var containerName = GetRequiredConfigValue(blobConfig, "ContainerName");

        var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        var client = new BlobContainerClient(containerUri, new DefaultAzureCredential());

        return new OeisDozenalExpansionAzureBlobStore(client);
    });
}

builder.Services.AddHttpClient<IOeisDecimalExpansionDownloader, OeisDecimalExpansionDownloader>();

// because downloader captures an HttpClient-backed downloader, it should not be singleton
builder.Services.AddScoped<IOeisDozenalExpansionService, OeisDozenalExpansionService>();

builder.Services.AddWebApi<ExpansionsApi>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var version = ThisAssembly.Version;

    options.SwaggerDoc(version, new()
    {
        Version = version,
        Title = ThisAssembly.Title,
        Description = ThisAssembly.Description,
    });
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.TypeInfoResolver = DtoSerializationContext.Default;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var version = ThisAssembly.Version;

    options.SwaggerEndpoint($"/swagger/{version}/swagger.json", ThisAssembly.Title + ' ' + version);
});

app.MapGet("/", () => Results.Redirect("/swagger", preserveMethod: true))
    .ExcludeFromDescription(); // prevent showing up in Swagger

app.Map<ExpansionsApi>();

app.Run();

static string GetRequiredConfigValue(IConfiguration config, string key)
{
    var value = config[key];

    if (string.IsNullOrEmpty(value))
    {
        if (config is IConfigurationSection section)
        {
            throw new InvalidOperationException($"Required configuration value {section.Path}:{key} not found!");
        }

        throw new InvalidOperationException($"Required configuration value {key} not found!");
    }

    return value;
}

// used to prevent accidentally running a debug build in a production container environment
// debug builds may leak sensitive information
[Conditional("DEBUG")]
static void CheckDebugAllowed(IConfiguration config)
{
    const string allowFlag = "AllowDebugBuildInContainer";

    var isInContainer = config.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
    var allowDebug = config.GetValue<bool>(allowFlag);

    if (isInContainer && !allowDebug)
    {
        while (true)
        {
            Console.Error.WriteLine($"Cannot run a debug build in a container unless “{allowFlag}” is set!");

            // Rather than throw an error, which might cause the container manager to repeatedly try to restart the app,
            // simply go to sleep for a while
            Thread.Sleep(TimeSpan.FromMinutes(5));
        }
    }
}
