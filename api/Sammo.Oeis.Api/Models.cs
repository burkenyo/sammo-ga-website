#if !DEBUG
using System.Text.Json.Serialization;
#endif

namespace Sammo.Oeis.Api;

class OeisExpansionDto
{
    public string Id { get; }

    public string Name { get; }

    public string Expansion { get; }

    internal OeisExpansionDto(IOeisFractionalExpansion expansion)
    {
        Id = expansion.Id.ToString();
        Name = expansion.Name.ToString();
        Expansion = expansion.Expansion.ToString();
    }
}

class ErrorDto
{
    public string Message { get; }

    public object? Details { get; protected init; }

    internal ErrorDto(string message)
    {
        Message = message;
    }

    internal ErrorDto(Exception ex)
    {
        Message = ex.Message;
    }
}

class OeisClientErrorDto : ErrorDto
{
    public class OeisClientErrorDetails
    {
        public string Cause { get; }

        public string? Id { get; }

#if !DEBUG
        [JsonIgnore]
#endif
        public string? InnerException { get; }

        internal OeisClientErrorDetails(OeisClientException ex)
        {
            Cause = ex.Cause.ToString();
            Id = ex.Id?.ToString();
            InnerException = ex.InnerException?.Message;
        }
    }

    public new OeisClientErrorDetails Details { get; }

    internal OeisClientErrorDto(OeisClientException ex) : base(ex)
    {
        Details = new OeisClientErrorDetails(ex);
        base.Details = Details;
    }
}