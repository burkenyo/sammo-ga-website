using System.Diagnostics;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Sammo.Oeis;
using Sammo.Oeis.Api;

/// <summary>
/// Contains extension methods for adding configuration sources,
/// registering services, and configuring the request pipeline.
/// </summary>
static class StartupExtensions
{
    public static void AddOeisDozenalExpansionFileStore(
        this IServiceCollection services, Config.FileStoreConfig config, out DirectoryInfo dataDir)
    {
        var dataDirPath = config.Require(c => c.DataDirectory);
        dataDir = new DirectoryInfo(dataDirPath);

        if (!dataDir.Exists)
        {
            dataDir.Create();
        }

        var fileStore = new OeisDozenalExpansionFileStore(dataDir);
        services.AddSingleton<IOeisDozenalExpansionStore>(fileStore);
    }

    public static void AddLocalTestingOeisDozenalExpansionService(
        this IServiceCollection services, DirectoryInfo dataDir)
    {
        services.AddScoped<IOeisDozenalExpansionService>(provider =>
            ActivatorUtilities.CreateInstance<LocalTestingOeisDozenalExpansionService>(provider, dataDir));
    }

    public static void AddKeyVaultStoredConfiguration(
        this ConfigurationManager configManager, Config.AzureConfig config, TokenCredential cred)
    {
        var keyVaultName = config.Require(c => c.KeyVaultName);
        var configSecretName = ThisAssembly.Name.Replace('.', '-').ToLower() + "--config";

        configManager.AddAzureKeyVaultJsonSecret(keyVaultName, configSecretName, cred);
    }

    public static void AddOeisDozenalExpansionAzureBlobStore(
        this IServiceCollection services, Config.AzureConfig.BlobsConfig blobsConfig, TokenCredential cred)
    {
        var accountName = blobsConfig.Require(c => c.AccountName);
        var containerName = blobsConfig.Require(c => c.ContainerName);

        var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");

        services.AddSingleton(new BlobContainerClient(containerUri, cred));
        services.AddSingleton<IOeisDozenalExpansionStore, OeisDozenalExpansionAzureBlobStore>();
    }

    public static void AddThisAssemblySwaggerGen(this IServiceCollection services) =>
        services.AddSwaggerGen(options =>
        {
            var version = ThisAssembly.Version;

            options.SwaggerDoc(version, new()
            {
                Version = version,
                Title = ThisAssembly.Title,
                Description = ThisAssembly.Description,
            });
        });

    public static void AddCors(this IServiceCollection services, Config.CorsConfig corsConfig) =>
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            // the ExpansionsApi uses the location header to indicate where to request the full expansion
            policy.WithExposedHeaders(HeaderNames.ContentLocation);

            if (corsConfig.AllowAnyOrigin)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                var allowedOrigins = corsConfig.AllowedOrigins
                    // GetLeftPart(UriPartial.Authority) returns the scheme and authority with no trailing slash
                    .Select(o => o.GetLeftPart(UriPartial.Authority))
                    .ToArray();

                policy.WithOrigins(allowedOrigins);
            }
        }));

    public static void ConfigureJsonOptions(this IServiceCollection services) =>
        services.ConfigureHttpJsonOptions(static options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.TypeInfoResolver = DtoSerializerContext.Default;
        });

    public static void UseThisAssemblySwaggerUi(this WebApplication app) =>
        app.UseSwaggerUI(static options =>
        {
            var version = ThisAssembly.Version;

            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", ThisAssembly.Title + ' ' + version);
        });

    public static void UseDataDirStaticFiles(this WebApplication app, DirectoryInfo dataDir) =>
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(dataDir.FullName),
            RequestPath = LocalTestingOeisDozenalExpansionService.UriPath
        });

    public static void MapRootToSwagger(this WebApplication app) =>
        app.MapGet("/", () => Results.Redirect("/swagger", preserveMethod: true))
            .ExcludeFromDescription(); // prevent showing up in Swagger

}

/// <summary>
/// Contains program startup utility methods
/// </summary>
static class Startup
{
    public static TokenCredential GetAzureCredential(Config.AzureConfig azureConfig)
    {
        if (azureConfig.UseClientSecretCredential is true)
        {
            ClientSecretCredentialOptions options = new();
            options.AdditionallyAllowedTenants.Add("*");

            return new ClientSecretCredential(
                azureConfig.TenantName, azureConfig.ClientId, azureConfig.ClientSecret, options);
        }

        DefaultAzureCredentialOptions options2 = new();
        options2.AdditionallyAllowedTenants.Add("*");

        return new DefaultAzureCredential(options2);
    }

    /// <summary>
    /// Used to prevent accidentally running a debug build in a production container environment,
    /// as debug builds may leak sensitive information.
    /// </summary>
    [Conditional("DEBUG")]
    public static void CheckDebugAllowed(IWebHostEnvironment env, Config config)
    {
        if (env.IsRunningInContainer() && !config.AllowDebugBuildInContainer)
        {
            throw new InvalidOperationException(
                $"Cannot run a debug build in a container unless “{nameof(Config.AllowDebugBuildInContainer)}” is set!");
        }
    }

    public static void LogError(IServiceProvider services, IWebHostEnvironment env, Exception ex)
    {
        var factory = services.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger(ThisAssembly.Name);

        logger.LogError(ex,"Error during startup!");

        if (env.IsRunningInContainer())
        {
            for (var i = 0; i < 5; i++)
            {
                // Rather than throw an error, which might cause the container manager to repeatedly try to restart the app,
                // simply go to sleep for a while and rewrite the message
                Thread.Sleep(TimeSpan.FromMinutes(1));
                logger.LogError(ex.Message);
            }
        }
    }
}