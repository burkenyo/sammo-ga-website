using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sammo.Oeis;

[assembly: FunctionsStartup(typeof(Sammo.Azure.Startup))]

namespace Sammo.Azure;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var accountName = "roflninjastorage";
        var containerName = "sammo-ga";

        var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        var containerClient = new BlobContainerClient(containerUri, new DefaultAzureCredential());

        builder.Services.AddSingleton(containerClient);

        builder.Services.AddSingleton<IOeisDozenalExpansionStore, OeisDozenalExpansionAzureBlobStore>();
        builder.Services.AddSingleton<IOeisDecimalExpansionDownloader, OeisDecimalExpansionDownloader>();
        builder.Services.AddSingleton<IOeisDozenalExpansionService, OeisDozenalExpansionService>();
    }
}

public class Functions
{
    readonly IOeisDozenalExpansionService _expansionService;

    public Functions(IOeisDozenalExpansionService expansionService)
    {
        _expansionService = expansionService;
    }

    [FunctionName(nameof(GetExpansionAsync))]
    public async Task<IActionResult> GetExpansionAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "expansions/{id}")] HttpRequest req,
        string id, ILogger log)
    {
        if (!OeisId.TryParse(id, out var oeisId, OeisId.ParseOption.Lax))
        {
            return new BadRequestObjectResult(new
            {
                message = "Invalid id value! Id should be ‘A’ followed by a positive integer."
            });
        }

        try
        {
            var expansion = await _expansionService.RetrieveAsync(oeisId);

            return new OkObjectResult(new
            {
                id = oeisId.ToString(),
                name = expansion!.Name,
                expansion = expansion.Expansion.ToString()
            });
        }
        catch (OeisClientException ex)
        {
            log.LogError(ex, $"{nameof(OeisClientException)} occurred. The error was reported to the client.");

            return new ObjectResult(new
            {
                message = ex.Message
            })
            {
                StatusCode = ex.Cause == OeisClientExceptionCause.InvalidSequence
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError
            };
        }
    }
}
