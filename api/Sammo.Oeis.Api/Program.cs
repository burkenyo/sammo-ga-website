using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.Json;
using Sammo.Oeis;
using Sammo.Oeis.Api;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

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
    var credOptions = new DefaultAzureCredentialOptions();
    credOptions.AdditionallyAllowedTenants.Add("*");
    var cred = new DefaultAzureCredential(credOptions);
    
    AddKeyVaultStoredConfiguration(cred);
    
    builder.Services.AddSingleton<IOeisDozenalExpansionStore>(provider =>
    {
        var blobConfig = config.GetRequiredSection("Azure:Blobs");

        var accountName = GetRequiredConfigValue(blobConfig, "AccountName");
        var containerName = GetRequiredConfigValue(blobConfig, "ContainerName");

        var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        var client = new BlobContainerClient(containerUri, cred);

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

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyOrigin();
}));

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.TypeInfoResolver = DtoSerializationContext.Default;
});

var app = builder.Build();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var version = ThisAssembly.Version;

    options.SwaggerEndpoint($"/swagger/{version}/swagger.json", ThisAssembly.Title + ' ' + version);
});

app.MapGet("/", () => Results.Redirect("/swagger", preserveMethod: true))
    .ExcludeFromDescription(); // prevent showing up in Swagger

app.Map<ExpansionsApi>();

CheckDebugAllowed();

app.Run();

void AddKeyVaultStoredConfiguration(DefaultAzureCredential cred)
{
    try
    {
        var keyVaultName = GetRequiredConfigValue(config, "Azure:KeyVaultName");
        var configSecretName = ThisAssembly.Name.Replace('.', '-').ToLower() + "--config";
        
        config.AddAzureKeyVaultJsonSecret(keyVaultName, configSecretName, cred);
    }
    catch (Exception ex)
    {
        LogErrorAndBail(ex.Message);
    }
}

string GetRequiredConfigValue(IConfiguration config, string key)
{
    var value = config[key];

    if (string.IsNullOrEmpty(value))
    {
        var message = config is IConfigurationSection section
            ? $"Required configuration value {section.Path}:{key} not found!"
            : $"Required configuration value {key} not found!";
        
        LogErrorAndBail(message);
    }

    return value!;
}

// used to prevent accidentally running a debug build in a production container environment
// debug builds may leak sensitive information
void CheckDebugAllowed()
{
#if DEBUG
    const string allowFlag = "AllowDebugBuildInContainer";

    var isInContainer = builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
    var allowDebug = builder.Configuration.GetValue<bool>(allowFlag);

    if (isInContainer && !allowDebug)
    {
        LogErrorAndBail($"Cannot run a debug build in a container unless “{allowFlag}” is set!");
    }
#endif
}

[SuppressMessage("ASP", "ASP0000:DoNotBuildServiceProvidePriorToRun",
    Justification = "I need to grab an ILogger, and the program is about to end anyway.")]
void LogErrorAndBail(string message)
{
    var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger(ThisAssembly.Name);

    logger.LogError(message);
    
    if (builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
    {
        for (var i = 0; i < 5; i++)
        {
            // Rather than throw an error, which might cause the container manager to repeatedly try to restart the app,
            // simply go to sleep for a while and rewrite the message
            Thread.Sleep(TimeSpan.FromMinutes(1));
            logger.LogError(message);
        }
    }

    Environment.Exit(1);
}