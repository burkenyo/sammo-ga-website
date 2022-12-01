using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Sammo.Oeis.Api;

class ExpansionsApi : IWebApi
{
    class OkWithContentLocation: IResult, IStatusCodeHttpResult,
        IValueHttpResult, IValueHttpResult<StoredOeisExpansionInfoDto>
    {
        private readonly Ok<StoredOeisExpansionInfoDto> _result;

        public OkWithContentLocation(StoredOeisExpansionInfo value)
        {
            _result = TypedResults.Ok(new StoredOeisExpansionInfoDto(value));
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.ContentLocation = Value.Uri.ToString();
            return _result.ExecuteAsync(httpContext);
        }

        public int StatusCode =>
            Status200OK;

        int? IStatusCodeHttpResult.StatusCode =>
            StatusCode;

        public StoredOeisExpansionInfoDto Value =>
            _result.Value!;

        object IValueHttpResult.Value =>
            Value;
    }

    public static void MapRoutes(IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("dozenalExpansions")
            .WithTags("Dozenal Expansions");

        group.MapGet("byId/{id}", (ExpansionsApi api, string id) => api.GetById(id))
            .WithSummary("Returns the dozenal expansion of an OEIS Sequence by its ID.")
            .WithParameterDescription("id", "The ID of the OEIS Sequence")
            .Produces<StoredOeisExpansionInfoDto>(Status200OK,
                "Info about the requested dozenal expansion. "
                    + "The Content-Location header indicates where the full expansion can be found.")
            .Produces<OeisClientErrorDto>(Status400BadRequest,
                "The Id parameter is invalid "
                + "or represents an OEIS Sequence that cannot be interpreted as a dozenal expansion.")
            .Produces<OeisClientErrorDto>(Status404NotFound,
                "No OEIS Sequence can be found by the requested ID.");

        group.MapGet("random", (ExpansionsApi api) => api.GetRandom())
            .WithSummary("Returns the dozenal expansion of a random OEIS Sequence.")
            .Produces<StoredOeisExpansionInfoDto>(Status200OK,
                "Info about a random dozenal expansion. "
                + "The Content-Location header indicates where the full expansion can be found.");
    }

    readonly IOeisDozenalExpansionService _expansionService;
    readonly ILogger _logger;

    public ExpansionsApi(IOeisDozenalExpansionService expansionService, ILogger<ExpansionsApi> logger)
    {
        _expansionService = expansionService;
        _logger = logger;
    }

    async Task<IResult> GetById(string id)
    {

        if (!OeisId.TryParse(id, out var oeisId, OeisId.ParseOption.Lax))
        {
            var message = "Invalid ID value! ID should be a value comprising ‘A’ followed by a positive integer "
                + $"less than {OeisId.MaxValue + 1:N0}.";

            return Results.BadRequest(new ErrorDto(message));
        }

        try
        {
            var info = await _expansionService.GetInfoAsync(oeisId);

            _logger.LogInformation("Returning expansion info for {id}.", info.Id);

            return new OkWithContentLocation(info);
        }
        catch (OeisClientException ex)
        {
            return HandleOeisClientException(ex);
        }
    }

    async Task<IResult> GetRandom()
    {
        try
        {
            var expansion = await _expansionService.GetInfoForRandomAsync();

            _logger.LogInformation("Returning expansion info for {id}.", expansion.Id);

            return new OkWithContentLocation(expansion);
        }
        catch (OeisClientException ex)
        {
            return HandleOeisClientException(ex);
        }
    }

    IResult HandleOeisClientException(OeisClientException ex)
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
                return Results.BadRequest(error);

            case OeisClientExceptionCause.NotFound:
                return Results.NotFound(error);
        }

        // Use this utility to keep the original stack trace in the exception.
        ExceptionDispatchInfo.Throw(ex);

        // never reached
        return null!;
    }
}