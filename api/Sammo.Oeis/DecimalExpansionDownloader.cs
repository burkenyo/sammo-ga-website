// Copyright © 2024 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

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

public partial class DecimalExpansionDownloader : IDecimalExpansionDownloader
{
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
            builder.Append("&n=1&fmt=json");

            return builder.ToString();
        }
    }

    sealed class SearchResult
    {
        public static readonly SearchResult Empty = new(0, 0, null);

        public int TotalCount { get; }

        public int Index { get; }

        public OeisSequence? Sequence { get; }

        public SearchResult(int totalCount, int index, OeisSequence? sequence)
        {
            Debug.Assert(totalCount > 0 ^ sequence is null,
                $"{nameof(totalCount)} must be 0 and {nameof(sequence)} must be null "
                + $"or {nameof(totalCount)} must be > 0 and {nameof(sequence)} must be not null!");

            TotalCount = totalCount;
            Index = index;
            Sequence = sequence;
        }

        public static async Task<SearchResult> FromQueryAsync(HttpClient client, QueryBuilder query)
        {
            Debug.WriteLine("Executing query: " + query);

            try
            {
                await using var stream = await client.GetStreamAsync(query.ToString());
                using var jsonDoc = await JsonDocument.ParseAsync(stream);
                var json = jsonDoc.RootElement;

                var totalCount = json.GetProperty("count").GetInt32();

                if (totalCount == 0)
                {
                    return Empty;
                }

                var index = json.GetProperty("start").GetInt32();

                var result = json.GetProperty("results")[0];

                var id = (OeisId) result.GetProperty("number").GetInt32();

                var name = result.GetProperty("name").GetString()
                    ?? throw OeisClientException.ParseError($"Could not parse the name of OEIS Sequence {id}", id);

                var offsetPair = result.GetProperty("offset").GetString()!;
                var offset = ParseOffset(id, offsetPair);

                return new SearchResult(totalCount, index, new OeisSequence(id, name, offset));
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is IOException)
            {
                throw OeisClientException.IOError($"Could not execute a query against OEIS!", ex);
            }
            catch (Exception ex) when (ex.IsSystemTextJsonException())
            {
                throw OeisClientException
                    .ParseError($"OEIS query results were not returned in the expected JSON format!", ex);
            }

            static int ParseOffset(OeisId id, string offsetPair)
            {
                const int maxDigits = 5;
                const string maxNumber = "99,999";

                var offsetString = offsetPair.AsSpan(0, offsetPair.IndexOf(','));

                if (offsetString.Length > (offsetString[0] != '-' ? maxDigits + 1 : maxDigits))
                {
                    throw OeisClientException.InvalidSequence($"The offset for OEIS Sequence {id} is out of range! "
                        + $"The offset must be between -{maxNumber} and {maxNumber}.", id);
                }

                if (!Int32.TryParse(offsetString, out var offset))
                {
                    throw OeisClientException.ParseError($"Could not parse the sequence offset for {id}!", id);
                }

                return offset;
            }
        }
    }

    [GeneratedRegex(@"^[ \t]*-?[0-9]+[ \t]*(-?[0-9]{1,2})[0-9]*[ \t]*$")]
    private static partial Regex BFileLineParser { get; }

    static int? s_randomExpansionCount;

    readonly HttpClient _oeisClient;

    public DecimalExpansionDownloader(HttpClient oeisClient)
    {
        oeisClient.BaseAddress = new Uri("https://oeis.org");
        _oeisClient = oeisClient;
    }

    public async Task<OeisSequence> GetRandomSequence()
    {
        SearchResult? result = null;

        if (s_randomExpansionCount is null)
        {
            // initialize the count the first time the endpoint is called
            result = await SearchResult.FromQueryAsync(_oeisClient, GetDecimalExpansionQuery());

            s_randomExpansionCount = result.TotalCount;
        }

        var index = Random.Shared.Next((int) s_randomExpansionCount);

        // this allows us to avoid querying again if we already have the result from initializing the count
        // AND we want the same result that was returned then (i.e. random returned index = 0)
        if (result is null || index != result.Index)
        {
            var query = GetDecimalExpansionQuery()
                .AtIndex(index);

            result = await SearchResult.FromQueryAsync(_oeisClient, query);

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

        var results = await SearchResult.FromQueryAsync(_oeisClient, query);

        switch (results.TotalCount)
        {
            case 0:
                throw OeisClientException.NotFound($"No OEIS sequence was found for ID {id}!", id);
            case > 1:
                // indicates a problem with the search query
                throw OeisClientException.IOError($"More than a single OEIS sequence was found for ID {id}!", id);
        }

        var sequence = results.Sequence!;

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
            using var reader = new StreamReader(bFileContent);

            var parsedTermsAsyncEnum = reader.EnumerateLinesAsync()
                .Where(static l => l != "" && l[0] != '#')
                .Select(l =>
                {
                    var parsedTerm = BFileLineParser.Match(l).Groups[1].ValueSpan;

                    CheckBFileTerm(id, parsedTerm);

                    return (byte)(parsedTerm[0] - '0');
                });

            if (maxDigits is not null)
            {
                parsedTermsAsyncEnum = parsedTermsAsyncEnum.Take((int) maxDigits);
            }

            var parsedTerms = await parsedTermsAsyncEnum.ToListAsync();

            var digits = new Fractional.DigitArray(parsedTerms.Count, 10);

            digits.Fill(parsedTerms);

            return new OeisDecimalExpansion(sequence.Id, sequence.Name, BigDecimal.Create(digits, sequence.Offset));
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is IOException)
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
