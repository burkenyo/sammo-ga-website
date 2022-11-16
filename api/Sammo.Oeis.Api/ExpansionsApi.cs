using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Sammo.Oeis.Api;

// switch expressions do not need to cover values that are not contained by s_possibleOutputContentTypes,
// as the handler will have returned status 406 (not acceptable) before that point
#pragma warning disable CS8509

class ExpansionsApi : IWebApi
{
    public static void MapRoutes(IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("dozenalExpansions")
            .WithTags("Dozenal Expansions");

        var getById = group.MapGet("byId/{id}",
                static (ExpansionsApi api, HttpRequest request, string id) => api.GetById(request, id))
            .WithSummary("Returns the dozenal expansion of an OEIS Sequence by its ID.")
            .WithParameterDescription("id", "The ID of the OEIS Sequence")
            .Produces<OeisExpansionDto>(Status200OK, 
                "The requested dozenal expansion",
                s_possibleResultContentTypes)
            .Produces(Status406NotAcceptable,
                "The result cannot be returned in the requested content type.")
            .Produces<OeisClientErrorDto>(Status400BadRequest,
                "The Id parameter is invalid "
                + "or represents an OEIS Sequence that cannot be interpreted as a dozenal expansion.",
                s_possibleResultContentTypes)
            .Produces<OeisClientErrorDto>(Status404NotFound,
                "No OEIS Sequence can be found by the requested ID.",
                s_possibleResultContentTypes);

        var getRandom = group.MapGet("random", 
                static (ExpansionsApi api, HttpRequest request) => api.GetRandom(request))
            .WithSummary("Returns the dozenal expansion of a random OEIS Sequence.")
            .Produces<OeisExpansionDto>(Status200OK,
                "A random dozenal expansion",
                s_possibleResultContentTypes)
            .Produces(Status406NotAcceptable,
                "The result cannot be returned in the requested content type.");
    }

    private static readonly IReadOnlyList<string> s_possibleResultContentTypes = new[] { Text.Plain, Application.Json };
    
    readonly IOeisDozenalExpansionService _expansionService;
    readonly ILogger _logger;

    public ExpansionsApi(IOeisDozenalExpansionService expansionService, ILogger<ExpansionsApi> logger)
    {
        _expansionService = expansionService;
        _logger = logger;
    }

    async Task<IResult> GetById(HttpRequest request, string id)
    {
        if (!TryGetResultContentType(request, out var resultContentType))
        {
            return Results.StatusCode(Status406NotAcceptable);
        }

        if (!OeisId.TryParse(id, out var oeisId, OeisId.ParseOption.Lax))
        {
            var message = "Invalid ID value! ID should be a value comprising ‘A’ followed by a positive integer "
                + $"less than {OeisId.MaxValue + 1:N0}.";

            return BadRequest(new ErrorDto(message), resultContentType);
        }

        try
        {
            switch (resultContentType)
            {
                case Text.Plain:
                    var (_, uri) = await _expansionService.GetPlainTextUri(oeisId);

                    _logger.LogInformation("Returning plain-text URL for {id}.", oeisId);

                    return Results.Redirect(uri.ToString(), preserveMethod: true);

                case Application.Json:
                    var expansion = await _expansionService.RetrieveAsync(oeisId);

                    _logger.LogInformation("Returning expansion for {id} as json.", oeisId);

                    return Results.Ok(new OeisExpansionDto(expansion));
            }
        }
        catch (OeisClientException ex)
        {
            return HandleOeisClientException(ex, resultContentType);
        }

        // not reached
        return null!;
    }
    
    async Task<IResult> GetRandom(HttpRequest accept)
    {
        if (!TryGetResultContentType(accept, out var resultContentType))
        {
            return Results.StatusCode(Status406NotAcceptable);
        }

        try
        {
            switch (resultContentType)
            {
                case Text.Plain:
                    var (oeisId, uri) = await _expansionService.GetPlainTextUriForRandom();

                    _logger.LogInformation("Returning plain-text URL for {id}.", oeisId);

                    return Results.Redirect(uri.ToString(), preserveMethod: true);

                case Application.Json:
                    var expansion = await _expansionService.RetrieveRandomAsync();

                    _logger.LogInformation("Returning expansion for {id} as json.", expansion.Id);

                    return Results.Ok(new OeisExpansionDto(expansion));
            }
        }
        catch (OeisClientException ex)
        {
            return HandleOeisClientException(ex, resultContentType);
        }

        // not reached
        return null!;
    }

    bool TryGetResultContentType(HttpRequest request, [NotNullWhen(true)] out string? resultContentType)
    {
        if (!MediaTypeHeaderValue.TryParseList(request.Headers.Accept, out var values))
        {
            resultContentType = null;
            return false;
        }

        foreach (var acceptValue in values)
        {
            foreach (var possibleContentType in s_possibleResultContentTypes)
            {
                if (acceptValue.MatchesMediaType(possibleContentType))
                {
                    resultContentType = possibleContentType;
                    return true;
                }
            }
        }

        resultContentType = null;
        return false;
    }

    IResult HandleOeisClientException(OeisClientException ex, string resultContentType)
    {
        var error = new OeisClientErrorDto(ex);

        _logger.Log(LogLevel.Error, $"{nameof(OeisClientException)} occurred. ",
            ("cause: {cause}", error.Details.Cause),
            ("ID: {id}", error.Details.Id),
            ("reason: “{message}”", error.Message),
            ("inner exception: “{innerException}”", error.Details.InnerException));

        switch (ex.Cause)
        {
            case OeisClientExceptionCause.InvalidSequence:
                return BadRequest(error, resultContentType);

            case OeisClientExceptionCause.NotFound:
                return NotFound(error, resultContentType);
        }

        // Use this utility to keep the original stack trace in the exception.
        ExceptionDispatchInfo.Throw(ex);

        // never reached
        return null!;
    }

    IResult BadRequest(ErrorDto error, string resultContentType) =>
        resultContentType switch
        {
            Text.Plain =>
                Results.Text(error.Message, statusCode: Status400BadRequest),
            Application.Json =>
                Results.BadRequest(error)
        };

    IResult NotFound(ErrorDto error, string resultContentType) =>
        resultContentType switch
        {
            Text.Plain =>
                Results.Text(error.Message, statusCode: Status404NotFound),
            Application.Json =>
                Results.NotFound(error)
        };
}