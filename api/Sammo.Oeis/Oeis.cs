using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Sammo.Oeis;

[DebuggerStepThrough]
public readonly record struct OeisId : IComparable<OeisId>, ISpanParsable<OeisId>, ISpanFormattable
{
    public enum ParseOption
    {
        // String must match canonical form of ‘A’ + a positive integer.
        Strict,

        // Prefix of ‘A’ is optional.
        Lax
    }

    public const int MinValue = 1;

    public const int MaxValue = 999_999_999;

    public OeisId(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public const int MaxStringLength = 11;

    readonly public int Value { get; }

    public override string ToString()
    {
        // the maximum possible length is 10 chars:
        //   • the ‘A’ prefix
        //   • 9 chars for the MaxValue
        Span<char> buffer = stackalloc char[10];

        TryFormat(buffer, out var charsWritten);

        return new String(buffer[..charsWritten]);
    }


    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        // The canonical representation must always include ‘A’ with the value padded to 6 chars
        if (destination.Length < 7)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = 'A';

        var result = Value.TryFormat(destination[1..], out charsWritten, "D6");

        charsWritten++;
        return result;
    }

    internal string GetPaddedValue() =>
        Value.ToString("D6");

    public static OeisId Parse(ReadOnlySpan<char> value, ParseOption option = ParseOption.Strict) =>
        TryParse(value, out var oeisId, option)
            ? oeisId
            : throw new FormatException("Input string was not in the correct format!");

    public static bool TryParse(ReadOnlySpan<char> value, out OeisId id, ParseOption option = ParseOption.Strict)
    {
        if (value.Length >= 2 && (value[0] == 'A' || (option == ParseOption.Lax && value[0] == 'a')))
        {
            value = value[1..];
        }
        else if (value.Length == 0 || option == ParseOption.Strict)
        {
            id = default;
            return false;
        }

        if (Int32.TryParse(value, out var intVal) && intVal is >= MinValue and <= MaxValue)
        {
            id = new OeisId(intVal);
            return true;
        }

        id = default;
        return false;
    }

    public int CompareTo(OeisId other) =>
        Value.CompareTo(other.Value);

    static OeisId IParsable<OeisId>.Parse(string value, IFormatProvider? provider) =>
        Parse(value);

    static bool IParsable<OeisId>.TryParse(
        [NotNullWhen(true)] string? value, IFormatProvider? provider, out OeisId result) =>
        TryParse(value, out result);

    static OeisId ISpanParsable<OeisId>.Parse(ReadOnlySpan<char> value, IFormatProvider? provider) =>
        Parse(value);

    static bool ISpanParsable<OeisId>.TryParse(
        ReadOnlySpan<char> value, IFormatProvider? provider, out OeisId result) =>
        TryParse(value, out result);

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) =>
        ToString();

    bool ISpanFormattable.TryFormat(
        Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten);

    public static explicit operator int(OeisId id) =>
        id.Value;

    public static explicit operator OeisId(int value) =>
        new OeisId(value);
}

public interface IOeisSequence
{
    OeisId Id { get; }

    string Name { get; }
}

public class OeisSequence
{
    public OeisId Id { get; }

    public string Name { get; }

    internal int Offset { get; }

    internal OeisSequence(OeisId id, string name, int offset)
    {
        Id = id;
        Name = name;
        Offset = offset;
    }
}

public interface IOeisFractionalExpansion : IOeisSequence
{
    Fractional Expansion { get; }
}

public interface IOeisFractionalExpansion<T> : IOeisFractionalExpansion where T : Fractional
{
    new T Expansion { get; }
}

public class OeisDecimalExpansion : IOeisFractionalExpansion<BigDecimal>
{
    public OeisId Id { get; }

    public string Name { get; }

    public BigDecimal Expansion { get; }

    Fractional IOeisFractionalExpansion.Expansion =>
        Expansion;

    internal OeisDecimalExpansion(OeisId id, string name, BigDecimal expansion)
    {
        Id = id;
        Name = name;
        Expansion = expansion;
    }

    internal OeisDozenalExpansion ConvertToDozenal() =>
        new OeisDozenalExpansion(Id, Name, Dozenal.FromFractional(Expansion));
}

public class OeisDozenalExpansion : IOeisFractionalExpansion<Dozenal>
{
    public OeisId Id { get; }

    public string Name { get; }

    public Dozenal Expansion { get; }

    Fractional IOeisFractionalExpansion.Expansion =>
        Expansion;

    internal OeisDozenalExpansion(OeisId id, string name, Dozenal expansion)
    {
        Id = id;
        Name = name;
        Expansion = expansion;
    }
}

public enum OeisClientExceptionCause
{
    /// <summary>
    /// The sequence was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The sequence cannot be interpreted as a decimal expansion.
    /// </summary>
    InvalidSequence,

    /// <summary>
    /// An error occurred downloading the sequence data or retrieving it from storage.
    /// </summary>
    IOError,

    /// <summary>
    /// The sequence data could not interpreted in the expected format.
    /// </summary>
    ParseError

}

/// <summary>
/// Indicates a problem occurred while retrieving sequence data from OEIS or storage.
/// </summary>
public class OeisClientException : Exception
{
    public OeisClientExceptionCause Cause { get; }

    public OeisId? Id { get; }

    OeisClientException(OeisClientExceptionCause cause, string message, OeisId? id,
        Exception? innerException) : base(message, innerException)
    {
#if DEBUG
        var mustIncludeId = cause == OeisClientExceptionCause.NotFound || cause == OeisClientExceptionCause.NotFound;

        Debug.Assert(!mustIncludeId || id?.Value != 0, $"Id must be specified for cause {cause}!");
#endif

        Cause = cause;
        Id = id;
    }

    public static OeisClientException NotFound(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(OeisClientExceptionCause.NotFound, message, id, innerException);

    public static OeisClientException InvalidSequence(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(OeisClientExceptionCause.InvalidSequence, message, id, innerException);

    public static OeisClientException IOError(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(OeisClientExceptionCause.IOError, message, id, innerException);

    public static OeisClientException IOError(string message, Exception? innerException = null) =>
        new OeisClientException(OeisClientExceptionCause.IOError, message, null, innerException);

    public static OeisClientException ParseError(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(OeisClientExceptionCause.ParseError, message, id, innerException);

    public static OeisClientException ParseError(string message, Exception? innerException = null) =>
        new OeisClientException(OeisClientExceptionCause.ParseError, message, null, innerException);
}

/// <summary>
/// Downloads decimal expansions from the The On-Line Encyclopedia of Integer Sequences® (OEIS®)
/// </summary>
public interface IOeisDecimalExpansionDownloader
{
    Task<OeisDecimalExpansion> DownloadAsync(OeisId id, int? maxDigits = null);

    Task<OeisDecimalExpansion> HydrateAsync(OeisSequence sequence, int? maxDigits = null);

    Task<OeisSequence> GetRandomSequence();
}


public partial class OeisDecimalExpansionDownloader : IOeisDecimalExpansionDownloader
{
    class QueryBuilder
    {
        int _index = 0;

        readonly Dictionary<string, object> _filters = new();

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
                    case OeisId id:
                        builder.Append('A');
                        builder.Append(id.Value);

                        break;
                    case string s:
                        builder.Append(quote);
                        builder.Append(s);
                        builder.Append(quote);

                        break;
                    case ISpanFormattable f:
                        builder.Append(f);

                        break;
#if DEBUG
                    default:
                        Debug.Assert(false, "Unhandled filter object type!");
                        break;
#endif
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
            Debug.WriteLine("Executing query: " + query.ToString());

            try
            {
                using var stream = await client.GetStreamAsync(query.ToString());
                using var jsonDoc = await JsonDocument.ParseAsync(stream);
                var _json = jsonDoc.RootElement;

                var totalCount = _json.GetProperty("count").GetInt32();

                if (totalCount == 0)
                {
                    return Empty;
                }

                var index = _json.GetProperty("start").GetInt32();

                var result = _json.GetProperty("results")[0];

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
    private static partial Regex GetBFileLineRegex();

    static int? s_randomExpansionCount;

    readonly HttpClient _oeisClient;

    public OeisDecimalExpansionDownloader(HttpClient oeisClient)
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

            s_randomExpansionCount = result!.TotalCount;
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

        if (maxDigits < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDigits));
        }

        try
        {
            using var bFileContent = await _oeisClient.GetStreamAsync($"/{id}/b{id.GetPaddedValue()}.txt");
            using var reader = new StreamReader(bFileContent);

            var parsedTermsAsyncEnum = reader.EnumerateLinesAsync()
                .Where(static l => l != "" && l[0] != '#')
                .Select(l =>
                {
                    var parsedTerm = GetBFileLineRegex().Match(l).Groups[1].ValueSpan;

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
        switch (parsedTerm.Length)
        {
            case 0:
                // indicates the format of the b-file has changed
                throw OeisClientException.ParseError($"Could not to parse the b-file for {id}!", id);

            case > 1:
                if (parsedTerm[0] == '-')
                {
                    throw OeisClientException.InvalidSequence(
                        $"Could not interpret OEIS sequence {id} as a decimal expansion! "
                            + "The sequence contains one or more terms that are negative.", id);
                }

                throw OeisClientException.InvalidSequence(
                    $"Could not interpret OEIS sequence {id} as a decimal expansion! "
                        + "The sequence contains one or more terms that are more than a single decimal digit.", id);
        }
    }
}

/// <summary>
/// Stores and retrieves OEIS fractional expansions that have been converted from decimal to dozenal.
/// </summary>
public interface IOeisDozenalExpansionStore
{
    /// <summary>
    /// Canned exceptions for use by implementers of IOeisDozenalExpansionStore
    /// </summary>
    public static class Errors
    {
        public static class IO
        {
            public static OeisClientException Retrieve(OeisId id, Exception? innerException = null) =>
                OeisClientException.IOError(
                    $"Could not retrieve a stored dozenal expansion for {id}!", id, innerException);

            public static OeisClientException Store(OeisId id, Exception innerException) =>
                OeisClientException.IOError(
                    $"Could not store a dozenal expansion for {id}!", id, innerException);
        }

        public static class BadSequenceList
        {
            public static OeisClientException Exists(OeisId id, Exception innerException) =>
                OeisClientException.IOError(
                    $"Could not add {id} to the bad sequence list!", id, innerException);

            public static OeisClientException Add(OeisId id, Exception innerException) =>
                OeisClientException.IOError(
                    $"Could not add {id} to the bad sequence list!", id, innerException);
        }

        public static OeisClientException NotFound(OeisId id) =>
            OeisClientException.NotFound(
                $"No stored dozenal expansion exists for {id}!", id);

        public static OeisClientException Parse(OeisId id, Exception innerException) =>
            OeisClientException.ParseError(
                $"Could not parse a stored dozenal expansion for {id}!", id, innerException);

    }

    Task<Uri> StoreAsync(OeisDozenalExpansion expansion);

    Task<bool> ExistsAsync(OeisId id);

    Task<Uri> GetUriAsync(OeisId id);

    Task<(bool, Uri?)> TryGetUriAsync(OeisId id);

    Task<OeisDozenalExpansion> RetrieveAsync(OeisId id);

    Task<(bool success, OeisDozenalExpansion? expansion)> TryRetrieveAsync(OeisId id);

    Task<(bool result, string? reason)> BadSequenceListContainsAsync(OeisId id);

    Task AddToBadSequenceListAsync(OeisId id, string reason);
}

/// <summary>
/// Reads and writes <see cref="OeisDozenalExpansion" />s using a simple text-based format, where:<para />
///     • The first line is the OEIS Sequence ID<para />
///     • The second line is the name/description of the sequence.<para />
///     • The third line is the expansion terms as converted to dozenal.
/// </summary>
public static class OeisDozenalExpansionSerializer
{
    public static async Task<OeisDozenalExpansion> ReadFromAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        var idString = await reader.ReadLineAsync()
            ?? throw new IOException("Could not read the OEIS Sequence ID of the expansion!");
        var id = OeisId.Parse(idString);

        var name = await reader.ReadLineAsync()
            ?? throw new IOException("Could not read the name of the expansion!");

        var dozenalString = await reader.ReadLineAsync()
            ?? throw new IOException("Could not read the digit sequence of the expansion!");
        var dozenal = Dozenal.Parse(dozenalString);

        if (await reader.ReadLineAsync() is not null)
        {
            throw new IOException("Unexpected lines in serialized expansion format!");
        }

        return new OeisDozenalExpansion(id, name, dozenal);
    }

    public static async Task WriteToAsync(OeisDozenalExpansion expansion, Stream stream)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true)
        {
            NewLine = "\n"
        };

        await writer.WriteLineAsync(expansion.Id.ToString());
        await writer.WriteLineAsync(expansion.Name);
        await writer.WriteLineAsync(expansion.Expansion.ToString(maxDigits: null));
        await writer.FlushAsync();
    }
}

/// <summary>
/// Reads and writes the list of OEIS Sequence known not to be valid decimal expansions
/// using a simple text-based format, where each line is:<para />
///     • The sequence ID, then<para />
///     • A colon followed by a single space, then<para />
///     • The reason why the sequence is invalid.
/// </summary>
public static class OeisBadSequenceListUtil
{
    public static async Task<(bool result, string? reason)> BadSequenceListContainsAsync(Stream stream, OeisId id)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        await foreach (var line in reader.EnumerateLinesAsync())
        {
            var indexOfColon = line.IndexOf(':');

            var parsedId = OeisId.Parse(line.AsSpan(0, indexOfColon));

            if (parsedId == id)
            {
                // skip the colon and the space
                return (true, line[(indexOfColon + 2)..]);
            }
        }

        return default;
    }

    public static async Task AddToBadSequenceList(Stream stream, OeisId id, string reason)
    {
        using var writer = new StreamWriter(stream,  leaveOpen: true)
        {
            NewLine = "\n"
        };

        await writer.WriteAsync(id.ToString());
        await writer.WriteAsync(": ");
        await writer.WriteLineAsync(reason);
        await writer.FlushAsync();
    }
}

public class OeisDozenalExpansionFileStore : IOeisDozenalExpansionStore
{
    readonly DirectoryInfo _directory;

    public OeisDozenalExpansionFileStore(DirectoryInfo directory)
    {
        _directory = directory;
    }

    FileInfo GetFile(OeisId id) =>
        new FileInfo(Path.Combine(_directory.FullName, id.ToString() + ".txt"));

    public Task<Uri> GetUriAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            throw IOeisDozenalExpansionStore.Errors.NotFound(id);
        }

        return Task.FromResult(new Uri(file.FullName));
    }

    public Task<(bool, Uri?)> TryGetUriAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            return Task.FromResult<(bool, Uri?)>((false, null));
        }

        return Task.FromResult<(bool, Uri?)>((true, new Uri(file.FullName)));
    }


    public Task<bool> ExistsAsync(OeisId id) =>
        Task.FromResult(GetFile(id).Exists);

    static async Task<OeisDozenalExpansion> RetrieveAsyncInternal(OeisId id, FileInfo file)
    {
        try
        {
            using var stream = file.OpenRead();
            var expansion = await OeisDozenalExpansionSerializer.ReadFromAsync(stream);

            if (expansion.Id != id)
            {
                throw IOeisDozenalExpansionStore.Errors.IO.Retrieve(id);
            }

            return expansion;
        }
        catch (IOException ex)
        {
            throw IOeisDozenalExpansionStore.Errors.IO.Retrieve(id, ex);
        }
        catch (FormatException ex)
        {
            throw IOeisDozenalExpansionStore.Errors.Parse(id, ex);
        }
    }

    public Task<OeisDozenalExpansion> RetrieveAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            throw IOeisDozenalExpansionStore.Errors.NotFound(id);
        }

        return RetrieveAsyncInternal(id, file);
    }

    public async Task<Uri> StoreAsync(OeisDozenalExpansion expansion)
    {
        var file = GetFile(expansion.Id);

        try
        {
            using var stream = file.OpenWrite();
            await OeisDozenalExpansionSerializer.WriteToAsync(expansion, stream);

            return new Uri(file.FullName);
        }
        catch (IOException ex)
        {
            throw IOeisDozenalExpansionStore.Errors.IO.Store(expansion.Id, ex);
        }
    }

    public async Task<(bool success, OeisDozenalExpansion? expansion)> TryRetrieveAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            return (false, null);
        }

        return (true, await RetrieveAsyncInternal(id, file));
    }

    string GetBadSequenceListPath() =>
        Path.Combine(_directory.FullName, "bad.txt");

    public async Task<(bool result, string? reason)> BadSequenceListContainsAsync(OeisId id)
    {
        try
        {
            using var stream = File.Open(GetBadSequenceListPath(), FileMode.OpenOrCreate, FileAccess.Read);

            return await OeisBadSequenceListUtil.BadSequenceListContainsAsync(stream, id);
        }
        catch (IOException ex)
        {
            throw IOeisDozenalExpansionStore.Errors.BadSequenceList.Exists(id, ex);
        }
    }

    public async Task AddToBadSequenceListAsync(OeisId id, string message)
    {
        try
        {
            using var stream = File.Open(GetBadSequenceListPath(), FileMode.OpenOrCreate);

            if (await OeisBadSequenceListUtil.BadSequenceListContainsAsync(stream, id) is (true, _))
            {
                return;
            }

            // append the new item
            stream.Seek(0, SeekOrigin.End);
            await OeisBadSequenceListUtil.AddToBadSequenceList(stream, id, message);
        }
        catch (IOException ex)
        {
            throw IOeisDozenalExpansionStore.Errors.BadSequenceList.Add(id, ex);
        }
    }
}

public interface IOeisDozenalExpansionService
{
    Task<(OeisId, Uri)> GetPlainTextUri(OeisId id);

    Task<OeisDozenalExpansion> RetrieveAsync(OeisId id);

    Task<(OeisId, Uri)> GetPlainTextUriForRandom(int maxTries = 3);

    Task<OeisDozenalExpansion> RetrieveRandomAsync(int maxTries = 3);
}

public class OeisDozenalExpansionService : IOeisDozenalExpansionService
{
    private static readonly KeyedSemaphores<OeisId> s_locks = new();

    readonly IOeisDecimalExpansionDownloader _decimalExpansionDownloader;

    readonly IOeisDozenalExpansionStore _dozenalExpansionStore;

    readonly ILogger? _logger;

    public OeisDozenalExpansionService(
        IOeisDecimalExpansionDownloader decimalExpansionDownloader,
        IOeisDozenalExpansionStore dozenalExpansionStore,
        ILogger<OeisDozenalExpansionService>? logger = null)
    {
        _decimalExpansionDownloader = decimalExpansionDownloader;
        _dozenalExpansionStore = dozenalExpansionStore;
        _logger = logger;
    }

    public async Task<(OeisId, Uri)> GetPlainTextUri(OeisId id)
    {
        if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, var message))
        {
            throw OeisClientException.InvalidSequence(message!, id);
        }

        // Try to find an expansion that was already downloaded and converted.
        if (await _dozenalExpansionStore.TryGetUriAsync(id) is (true, var expansionUri))
        {
            return (id, expansionUri!);
        }

        using var semaphore = s_locks.Borrow(id);
        await semaphore.WaitAsync();

        // Try again to find the expansion now that we have the semaphore.
        // This is predicated on the assumption that it’s cheaper to try to find the expansion again
        // if another request just downloaded and converted it than doing that here
        // and overwriting any stored result.
        if (await _dozenalExpansionStore.TryGetUriAsync(id) is (true, var expansionUri2))
        {
            return (id, expansionUri2!);
        };

        try
        {
            _logger?.LogInformation("{id} not found in store, attempting to download.", id);

            var expansion = (await _decimalExpansionDownloader.DownloadAsync(id)).ConvertToDozenal();

            expansionUri = await _dozenalExpansionStore.StoreAsync(expansion);

            return (id, expansionUri);
        }
        catch (OeisClientException ex) when (ex.Cause == OeisClientExceptionCause.InvalidSequence)
        {
            _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

            await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

            throw;
        }
    }

    public async Task<OeisDozenalExpansion> RetrieveAsync(OeisId id)
    {
        if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, var message))
        {
            throw OeisClientException.InvalidSequence(message!, id);
        }

        // Try to find an expansion that was already downloaded and converted.
        if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, var existingExpansion))
        {
            return existingExpansion!;
        }

        using var semaphore = s_locks.Borrow(id);
        await semaphore.WaitAsync();

        // Try again to find the expansion now that we have the semaphore.
        // This is predicated on the assumption that it’s cheaper to try to find the expansion again
        // if another request just downloaded and converted it than doing that here
        // and overwriting any stored result.
        if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, var existingExpansion2))
        {
            return existingExpansion2!;
        };

        try
        {
            _logger?.LogInformation("{id} not found in store, attempting to download.", id);

            var expansion = (await _decimalExpansionDownloader.DownloadAsync(id)).ConvertToDozenal();

            await _dozenalExpansionStore.StoreAsync(expansion);

            return expansion;
        }
        catch (OeisClientException ex) when (ex.Cause == OeisClientExceptionCause.InvalidSequence)
        {
            _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

            await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

            throw;
        }
    }

    public async Task<(OeisId, Uri)> GetPlainTextUriForRandom(int maxTries = 3)
    {
        if (maxTries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTries));
        }

        List<OeisClientException>? failures = null;

        for (var i = 0; i < maxTries; i++)
        {
            OeisSequence sequence;
            try
            {
                sequence = await _decimalExpansionDownloader.GetRandomSequence();
            }
            catch (OeisClientException ex) when (ex.Cause == OeisClientExceptionCause.InvalidSequence)
            {
                (failures ??= new()).Add(ex);

                _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

                continue;
            }

            var id = sequence.Id;

            if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, var message))
            {
                (failures ??= new()).Add(OeisClientException.InvalidSequence(message!, id));

                continue;
            }

            // Try to find an expansion that was already downloaded and converted.
            if (await _dozenalExpansionStore.TryGetUriAsync(id) is (true, var expansionUri))
            {
                return (id, expansionUri!);
            }

            using var semaphore = s_locks.Borrow(id);
            await semaphore.WaitAsync();

            // Try again to find the expansion now that we have the semaphore.
            // This is predicated on the assumption that it’s cheaper to try to find the expansion again
            // if another request just downloaded and converted it than doing that here
            // and overwriting any stored result.
            if (await _dozenalExpansionStore.TryGetUriAsync(id) is (true, var expansionUri2))
            {
                return (id, expansionUri2!);
            };

            try
            {
                _logger?.LogInformation("{id} not found in store, attempting to hydrate.", id);

                var expansion = (await _decimalExpansionDownloader.HydrateAsync(sequence)).ConvertToDozenal();

                expansionUri = await _dozenalExpansionStore.StoreAsync(expansion);

                return (id, expansionUri);
            }
            catch (OeisClientException ex) when (ex.Cause == OeisClientExceptionCause.InvalidSequence)
            {
                (failures ??= new()).Add(ex);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);
            }
        }

        throw OeisClientException.IOError($"Could not retrieve a valid sequence after {maxTries} tries!",
            new AggregateException(failures!));
    }

    public async Task<OeisDozenalExpansion> RetrieveRandomAsync(int maxTries = 3)
    {
        if (maxTries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTries));
        }

        List<OeisClientException>? failures = null;

        for (var i = 0; i < maxTries; i++)
        {
            OeisSequence sequence;
            try
            {
                sequence = await _decimalExpansionDownloader.GetRandomSequence();

            }
            catch (OeisClientException ex) when (ex.Cause == OeisClientExceptionCause.InvalidSequence)
            {
                (failures ??= new()).Add(ex);

                _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

                continue;
            }

            var id = sequence.Id;

            if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, var message))
            {
                (failures ??= new()).Add(OeisClientException.InvalidSequence(message!, id));

                continue;
            }

            // Try to find an expansion that was already downloaded and converted.
            if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, var existingExpansion))
            {
                return existingExpansion!;
            };

            using var semaphore = s_locks.Borrow(id);
            await semaphore.WaitAsync();

            // Try again to find the expansion now that we have the semaphore.
            // This is predicated on the assumption that it’s cheaper to try to find the expansion again
            // if another request just downloaded and converted it than doing that here
            // and overwriting any stored result.
            if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, var existingExpansion2))
            {
                return existingExpansion2!;
            };

            try
            {
                _logger?.LogInformation("{id} not found in store, attempting to hydrate.", id);

                var expansion = (await _decimalExpansionDownloader.HydrateAsync(sequence)).ConvertToDozenal();

                await _dozenalExpansionStore.StoreAsync(expansion);

                return expansion;
            }
            catch (OeisClientException ex) when (ex.Cause == OeisClientExceptionCause.InvalidSequence)
            {
                (failures ??= new()).Add(ex);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);
            }
        }

        throw OeisClientException.IOError($"Could not retrieve a valid sequence after {maxTries} tries!",
            new AggregateException(failures!));
    }
}
