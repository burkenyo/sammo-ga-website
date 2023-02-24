// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Text.Json.Serialization;

namespace Sammo.Oeis.Api;

[JsonSerializable(typeof(OeisExpansionDto))]
[JsonSerializable(typeof(OeisClientErrorDto))]
[JsonSerializable(typeof(StoredOeisExpansionInfoDto))]
[JsonSerializable(typeof(OeisExpansionDto))]
[JsonSerializable(typeof(GitInfoDto))]
partial class DtoSerializerContext : JsonSerializerContext { }

class OeisExpansionInfoDto
{
    public string Id { get; }

    public string Name { get; }

    public int Radix { get; }

    private protected OeisExpansionInfoDto(string id, string name, int radix)
    {
        Id = id;
        Name = name;
        Radix = radix;
    }
}

class StoredOeisExpansionInfoDto : OeisExpansionInfoDto
{
    public string Preview { get; }

    internal Uri Uri { get; }

    public StoredOeisExpansionInfoDto(StoredOeisExpansionInfo info) :
        base(info.Id.ToString(), info.Name, info.Radix)
    {
        Preview = info.Preview;
        Uri = info.Uri;
    }
}

class OeisExpansionDto : OeisExpansionInfoDto
{
    public string Expansion { get; }

    public OeisExpansionDto(IOeisFractionalExpansion expansion)
        : base(expansion.Id.ToString(), expansion.Name, expansion.Expansion.Radix)
    {
        Expansion = expansion.Expansion.ToString(maxDigits: null);
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

    private protected ErrorDto(Exception ex)
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

    public OeisClientErrorDto(OeisClientException ex) : base(ex)
    {
        Details = new OeisClientErrorDetails(ex);
        base.Details = Details;
    }
}

class GitInfoDto
{
    public string Branch =>
        ThisAssembly.Git.Branch;

    public string Commit =>
        ThisAssembly.Git.Commit;

    public bool IsDirty =>
        ThisAssembly.Git.IsDirty;
}
