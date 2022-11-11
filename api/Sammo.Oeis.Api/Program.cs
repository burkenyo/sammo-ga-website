using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Storage.Blobs;
using Sammo.Oeis;
using Sammo.Oeis.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using OeisExpansionOrErrorResult = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.BadRequest<Sammo.Oeis.Api.ErrorDto>,
    Microsoft.AspNetCore.Http.HttpResults.NotFound<Sammo.Oeis.Api.ErrorDto>,
    Microsoft.AspNetCore.Http.HttpResults.Ok<Sammo.Oeis.Api.OeisExpansionDto>>;

using static Microsoft.AspNetCore.Http.StatusCodes;

#pragma warning disable CS8524

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
            throw new DirectoryNotFoundException();
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
builder.Services.AddSingleton<IOeisDozenalExpansionService, OeisDozenalExpansionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("expansions/{id}",
    async Task<OeisExpansionOrErrorResult> (
        string id, [FromHeader] StringValues? accept, IOeisDozenalExpansionService expansionService) =>
{
    if (!TryGetResultContentType(accept, out var resultContentType))
    {
        return TypedResults.StatusCode(Status406NotAcceptable);
    }

    if (!OeisId.TryParse(id, out var oeisId, OeisId.ParseOption.Lax))
    {
        var message = "Invalid ID value! ID should be a value comprising ‘A’ followed by a positive integer.";

        return resultContentType switch
        {
            ResultContentType.TextPlain =>
                TypedResults.Content(message, statusCode: Status400BadRequest),
            ResultContentType.ApplicationJson =>
                TypedResults.BadRequest(new ErrorDto(message))
        };
    }

    try
    {
        switch (resultContentType)
        {
            case ResultContentType.TextPlain:
                var (_, uri) = await expansionService.GetPlainTextUri(oeisId);

                app.Logger.LogInformation("Returning plain-text URL for {id}.", oeisId);

                return TypedResults.Redirect(uri.ToString(), preserveMethod: true);
            case ResultContentType.ApplicationJson:
                var expansion = await expansionService.RetrieveAsync(oeisId);

                app.Logger.LogInformation("Returning expansion for {id} as json.", oeisId);

                return TypedResults.Ok(new OeisExpansionDto(expansion));
        }
    }
    catch (OeisClientException ex)
    {
        return HandleOeisClientException(ex, resultContentType);
    }

    // not reached
    return null!;
});

app.MapGet("randomExpansion",
    async Task<OeisExpansionOrErrorResult> (
        [FromHeader] StringValues? accept, IOeisDozenalExpansionService expansionService) =>
{
    if (!TryGetResultContentType(accept, out var resultContentType))
    {
        return TypedResults.StatusCode(Status406NotAcceptable);
    }

    try
    {
        switch (resultContentType)
        {
            case ResultContentType.TextPlain:
                var (oeisId, uri) = await expansionService.GetPlainTextUriForRandom();

                app.Logger.LogInformation("Returning plain-text URL for {id}.", oeisId);

                return TypedResults.Redirect(uri.ToString(), preserveMethod: true);
            case ResultContentType.ApplicationJson:
                var expansion = await expansionService.RetrieveRandomAsync();

                app.Logger.LogInformation("Returning expansion for {id} as json.", expansion.Id);

                return TypedResults.Ok(new OeisExpansionDto(expansion));
        }
    }
    catch (OeisClientException ex)
    {
        return HandleOeisClientException(ex, resultContentType);
    }

    // not reached
    return null!;
});

app.Run();

static bool TryGetResultContentType(
    [NotNullWhen(true)] StringValues? header, out ResultContentType resultContentType)
{
    if (!MediaTypeHeaderValue.TryParseList(header, out var values))
    {
        resultContentType = default;
        return false;
    }

    foreach(var acceptValue in values)
    {
        if (acceptValue.MatchesMediaType("text/plain"))
        {
            resultContentType = ResultContentType.TextPlain;
            return true;
        }

        if (acceptValue.MatchesMediaType("application/json"))
        {
            resultContentType = ResultContentType.ApplicationJson;
            return true;
        }
    }

    resultContentType = default;
    return false;
}

OeisExpansionOrErrorResult HandleOeisClientException(OeisClientException ex, ResultContentType resultContentType)
{
    var error = new OeisClientErrorDto(ex);

    app.Logger.Log(LogLevel.Error, $"{nameof(OeisClientException)} occurred. ",
        ("cause: {cause}", error.Details.Cause),
        ("ID: {id}", error.Details.Id),
        ("reason: “{message}”", error.Message),
        ("inner exception: “{innerException}”", error.Details.InnerException));

    switch (ex.Cause)
    {
        case OeisClientExceptionCause.InvalidSequence:
            return resultContentType switch
            {
                ResultContentType.TextPlain =>
                    TypedResults.Content(error.Message, statusCode: Status400BadRequest),
                ResultContentType.ApplicationJson =>
                    TypedResults.BadRequest((ErrorDto) error)
            };

        case OeisClientExceptionCause.NotFound:
            return resultContentType switch
            {
                ResultContentType.TextPlain =>
                    TypedResults.Content(error.Message, statusCode: Status404NotFound),
                ResultContentType.ApplicationJson =>
                    TypedResults.NotFound((ErrorDto) error)
            };
    }

    // Use this utility to keep the original stack trace in the exception.
    ExceptionDispatchInfo.Throw(ex);

    // never reached
    return null!;
}

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
    var isInContainer = config.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER");
    var allowDebug = config.GetValue<bool>("AllowDebugBuildInContainer");

    if (isInContainer && !allowDebug)
    {
        Console.Error.WriteLine("Cannot run a debug build in a container unless “AllowDebugInContainer” is set!");

        // Rather than throw an error, which might cause the container manager to repeatedly try to restart the app,
        // simply go to sleep forever
        Thread.Sleep(Timeout.Infinite);
    }
}

enum ResultContentType { TextPlain, ApplicationJson }
