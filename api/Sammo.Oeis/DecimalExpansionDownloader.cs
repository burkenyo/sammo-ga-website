// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Diagnostics;
using System.Text;
using Cysharp.IO;

namespace Sammo.Oeis;

/// <summary>
/// Downloads decimal expansions from the On-Line Encyclopedia of Integer Sequences® (OEIS®)
/// </summary>
public interface IDecimalExpansionDownloader
{
    Task<OeisDecimalExpansion> DownloadAsync(OeisId id, int? maxDigits = null);

    Task<OeisDecimalExpansion> HydrateAsync(OeisSequence sequence, int? maxDigits = null);

    Task<OeisSequence> GetRandomSequence();
}

public class DecimalExpansionDownloader : IDecimalExpansionDownloader
{
    async Task<OeisSearchResult> QueryOeisAsync(QueryBuilder query)
    {
        Debug.WriteLine("Executing query: " + query);

        try
        {
            await using var stream = await _oeisClient.GetStreamAsync(query.ToString());
            return await SeachResultParser.ParseAsync(stream);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw OeisClientException.IOError("Could not execute a query against OEIS!", ex);
        }
    }

    static int? s_randomExpansionCount;

    readonly HttpClient _oeisClient;

    public DecimalExpansionDownloader(HttpClient oeisClient)
    {
        oeisClient.BaseAddress = new Uri("https://oeis.org");
        _oeisClient = oeisClient;
    }

    public async Task<OeisSequence> GetRandomSequence()
    {
        OeisSearchResult? result = null;

        if (s_randomExpansionCount is null)
        {
            // initialize the count the first time the endpoint is called
            result = await QueryOeisAsync(GetDecimalExpansionQuery());

            s_randomExpansionCount = result.TotalCount;
        }

        var index = Random.Shared.Next((int) s_randomExpansionCount);

        // this allows us to avoid querying again if we already have the result from initializing the count
        // AND we want the same result that was returned then (i.e. random returned index = 0)
        if (result is null || index != result.Index)
        {
            var query = GetDecimalExpansionQuery()
                .AtIndex(index);

            result = await QueryOeisAsync(query);

            // keep the count current with each query
            s_randomExpansionCount = result.TotalCount;
        }

        Debug.Assert(result is not null && result.Index == index, "Result index is incorrect!");

        return result.Sequence!;

        static QueryBuilder GetDecimalExpansionQuery() =>
            new QueryBuilder()
                .WithKeyword("cons")
                .WithName("decimal%20expansion");
    }

    public async Task<OeisDecimalExpansion> DownloadAsync(OeisId id, int? maxDigits = null)
    {
        var query = new QueryBuilder()
            .WithId(id);

        var result = await QueryOeisAsync(query);

        switch (result.TotalCount)
        {
            case 0:
                throw OeisClientException.NotFound($"No OEIS sequence was found for ID {id}!", id);
            case > 1:
                // indicates a problem with the search query
                throw OeisClientException.IOError($"More than a single OEIS sequence was found for ID {id}!", id);
        }

        var sequence = result.Sequence!;

        return await HydrateAsync(sequence, maxDigits);
    }

    public async Task<OeisDecimalExpansion> HydrateAsync(OeisSequence sequence, int? maxDigits = null)
    {
        var id = sequence.Id;

        if (maxDigits is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan((int) maxDigits, 1);
        }

        try
        {
            await using var bFileContent = await _oeisClient.GetStreamAsync($"/{id}/b{id.GetPaddedValue()}.txt");
            var terms = await BFileParser.ParseAsync(id, bFileContent, maxDigits);

            var digits = new Fractional.DigitArray(terms.Count, 10);
            digits.Fill(terms);

            return new OeisDecimalExpansion(sequence.Id, sequence.Name, BigDecimal.Create(digits, sequence.Offset));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw OeisClientException.IOError($"Could not retrieve the b-file for {id} from OEIS!", id, ex);
        }
    }

    static void CheckBFileTerm(OeisId id, ReadOnlySpan<char> parsedTerm)
    {
        switch (parsedTerm)
        {
            case { Length: 0 }:
                // indicates the format of the b-file has changed
                throw OeisClientException.ParseError($"Could not to parse the b-file for {id}!", id);

            case ['-', ..]:
                throw OeisClientException.InvalidSequence(
                    $"Could not interpret OEIS sequence {id} as a decimal expansion! "
                        + "The sequence contains one or more terms that are negative.", id);

            case { Length: > 1 }:
                throw OeisClientException.InvalidSequence(
                    $"Could not interpret OEIS sequence {id} as a decimal expansion! "
                        + "The sequence contains one or more terms that are more than a single decimal digit.", id);
        }
    }
}

class QueryBuilder
{
    int _index;

    readonly Dictionary<string, object> _filters = [];

    public QueryBuilder WithId(OeisId id)
    {
        _filters.Add("id", id);

        return this;
    }

    public QueryBuilder WithName(string name)
    {
        _filters.Add("name", name);

        return this;
    }

    public QueryBuilder WithKeyword(string keyword)
    {
        _filters.Add("keyword", keyword);

        return this;
    }

    public QueryBuilder AtIndex(int index)
    {
        Debug.Assert(index >= 0, "Invalid page!");

        _index = index;

        return this;
    }

    public override string ToString()
    {
        Debug.Assert(_filters.Any(), "No filters added!");

        // url-encoded special chars
        const string colon = "%3A";
        const string space = "%20";
        const string quote = "%22";

        var builder = new StackStringBuilder();

        builder.Append("/search?q=");

        bool addSpace = false;
        foreach(var (key, value) in _filters)
        {
            if (addSpace)
            {
                builder.Append(space);
            }

            builder.Append(key);
            builder.Append(colon);

            switch (value)
            {
                case string s:
                    builder.Append(quote);
                    builder.Append(s);
                    builder.Append(quote);

                    break;
                case ISpanFormattable f:
                    builder.Append(f);

                    break;
                default:
                    Debug.Assert(false, "Unhandled filter object type!");
                    break;
            }

            addSpace = true;
        }

        builder.Append("&start=");
        builder.Append(_index);
        builder.Append("&n=1&fmt=text");

        return builder.ToString();
    }
}

static class SeachResultParser
{
    enum ParsePhase { IndexAndCount, IdAndName, Offset, Complete }

    public static async Task<OeisSearchResult> ParseAsync(Stream stream)
    {
        await using var reader = new Utf8StreamReader(stream, leaveOpen: true);

        ParsePhase parsePhase = ParsePhase.IndexAndCount;
        int index = 0;
        int count = 0;
        OeisId id = default;
        int idLength = 0;
        string? name = null;
        int offset = 0;
        await foreach (var line in reader.ReadAllLinesAsync())
        {
            var span = line.Span;
            switch (parsePhase)
            {
                case ParsePhase.IndexAndCount:
                    if (span.StartsWith("No results"u8))
                    {
                        return OeisSearchResult.Empty;
                    }

                    if (span.StartsWith("Showing"u8))
                    {
                        (index, count) = ParseIndexAndCount(span);
                        parsePhase = ParsePhase.IdAndName;
                    }
                    break;

                case ParsePhase.IdAndName:
                    if (span.StartsWith("%N"u8))
                    {
                        (id, idLength, name) = ParseIdAndName(span);
                        parsePhase = ParsePhase.Offset;
                    }

                    break;
                case ParsePhase.Offset:
                    if (span.StartsWith("%O"u8))
                    {
                        offset = ParseOffset(id, idLength, span);
                        parsePhase = ParsePhase.Complete;
                        goto finished_parsing;
                    }

                    break;
            }
        }

        finished_parsing:
        switch (parsePhase)
        {
            case ParsePhase.IndexAndCount:
                throw OeisClientException.ParseError("Could not find the search result index and count!");
            case ParsePhase.IdAndName:
                throw OeisClientException.ParseError("Could not find the search result id and name!");
            case ParsePhase.Offset:
                throw OeisClientException.ParseError(
                    $"Could not find the search result offset for and {id}!", id);
        }

        return new OeisSearchResult(index, count, new OeisSequence(id, name!, offset));
    }

    static (int index, int count) ParseIndexAndCount(ReadOnlySpan<byte> span)
    {
        // parse a line such as “Showing 1572-1572 of 12643”

        // The total is the integer between the final space and the end
        if (!Int32.TryParse(span[span.LastIndexOf((byte)' ')..], out var count))
        {
            throw OeisClientException.ParseError("Could not parse the search result index!");
        }

        // Chop the span off at the hyphen; the index is integer between then space and the new end
        span = span[..span.IndexOf((byte)'-')];
        if (!Int32.TryParse(span[span.LastIndexOf((byte)' ')..], out var ordinal))
        {
            throw OeisClientException.ParseError("Could not parse the search result total count!");
        }

        // The ordinal is 1-based, but the index is 0-based
        return (ordinal - 1, count);
    }

    static (OeisId id, int idLength, string name) ParseIdAndName(ReadOnlySpan<byte> span)
    {
        // parse a line such as “%N A019798 Decimal expansion of sqrt(2*e).”

        span = span[3..];
        var index =  span.IndexOf((byte)' ');
        if (!OeisId.TryParse(span[..index], out var id))
        {
            throw OeisClientException.ParseError("Could not parse the search result total count!");
        }

        var name = Encoding.UTF8.GetString(span[(index + 1)..]);
        return (id, index, name);
    }

    static int ParseOffset(OeisId id, int idLength, ReadOnlySpan<byte> span)
    {
        // parse a line such as “%O A019798 1,1”

        const int maxDigits = 5;
        const string maxNumber = "99,999";

        span = span[(4 + idLength)..span.IndexOf((byte)',')];

        if (span.Length > (span[0] != '-' ? maxDigits + 1 : maxDigits))
        {
            throw OeisClientException.InvalidSequence($"The offset for OEIS Sequence {id} is out of range! "
                + $"The offset must be between -{maxNumber} and {maxNumber}.", id);
        }

        if (!Int32.TryParse(span, out var offset))
        {
            throw OeisClientException.ParseError($"Could not parse the sequence offset for {id}!", id);
        }

        return offset;
    }
}

static class BFileParser
{
    public static async Task<List<byte>> ParseAsync(OeisId id, Stream stream, int? maxDigits)
    {
        await using var reader = new Utf8StreamReader(stream, leaveOpen: true);

        int current = 0;
        List<byte> terms = [];
        await foreach (var line in reader.ReadAllLinesAsync())
        {
            var span = line.Span;
            if (span is [] or [(byte)'#', ..])
            {
                continue;
            }

            span = span[(span.IndexOf((byte)' ') + 1)..];
            CheckBFileTerm(id, span);
            terms.Add((byte)(span[0] - '0'));

            if (maxDigits is not null && ++current == maxDigits)
            {
                break;
            }
        }

        return terms;
    }

    static void CheckBFileTerm(OeisId id, ReadOnlySpan<byte> span)
    {
        switch (span.Length)
        {
            case 0:
                // indicates the format of the b-file has changed
                throw OeisClientException.ParseError($"Could not to parse the b-file for {id}!", id);

            case > 1:
                throw OeisClientException.InvalidSequence(
                    $"Could not interpret OEIS sequence {id} as a decimal expansion! "
                    + "The sequence contains one or more terms that are more than a single decimal digit.", id);
        }

        switch (span[0])
        {
            case (byte)'-':
                throw OeisClientException.InvalidSequence(
                    $"Could not interpret OEIS sequence {id} as a decimal expansion! "
                    + "The sequence contains one or more terms that are negative.", id);

            case < (byte)'0' or > (byte)'9':
                throw OeisClientException.ParseError($"Could not to parse the b-file for {id}!", id);
        }
    }
}
