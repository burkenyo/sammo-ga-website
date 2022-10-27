using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;

namespace Sammo.Oeis;

using static OeisClientExceptionCause;

public readonly record struct OeisId(int Value)
{
    public enum ParseOption
    {
        // String must match canonical form of ‘A’ + a positive integer.
        Strict,

        // Prefix of ‘A’ is optional.
        Lax
    }

    public int Value { get; } = Value >= 1
        ? Value
        : throw new ArgumentOutOfRangeException(nameof(Value));

    [DebuggerStepThrough]
    public override string ToString() =>
        $"A{Value:D6}";

    public static OeisId Parse(ReadOnlySpan<char> value, ParseOption option = ParseOption.Strict) =>
        TryParse(value, out var oeisId)
            ? oeisId
            : throw new FormatException("Input string was not in the correct format!");

    public static bool TryParse(ReadOnlySpan<char> value, out OeisId id, ParseOption option = ParseOption.Strict)
    {
        if (value.Length > 2 && value[0] == 'A')
        {
            value = value[1..];
        }
        else if (option == ParseOption.Strict)
        {
            id = default;
            return false;
        }

        if (Int32.TryParse(value, out var intVal) && intVal >= 1)
        {
            id = new OeisId(intVal);
            return true;
        }

        id = default;
        return false;
    }

    public static implicit operator int(OeisId id) =>
        id.Value;

    public static explicit operator OeisId(int value) =>
        new OeisId(value);
}

public interface IOeisFractionalExpansion
{
    OeisId Id { get; }

    string Name { get; }

    Fractional Expansion { get; }
}

public class OeisDecimalExpansion : IOeisFractionalExpansion
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

public class OeisDozenalExpansion : IOeisFractionalExpansion
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
    /// The sequence does not exist or cannot be interpreted as a decimal expansion.
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

    public OeisClientException(OeisClientExceptionCause cause, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        if (!Enum.IsDefined(cause))
        {
            throw new ArgumentOutOfRangeException(nameof(cause));
        }

        Cause = cause;
    }
}

/// <summary>
/// Downloads decimal expansions from the The On-Line Encyclopedia of Integer Sequences® (OEIS®)
/// </summary>
public interface IOeisDecimalExpansionDownloader
{
    public Task<OeisDecimalExpansion> DownloadAsync(OeisId id, int? maxDigits = null);
}

public partial class OeisDecimalExpansionDownloader : IOeisDecimalExpansionDownloader
{
    const string s_bFileLineRegexString = @"^-?[0-9]+ +(-?[0-9]{1,2})[0-9]*$";

#if NET7_0
    [GeneratedRegex(s_bFileLineRegexString)]
    private static partial Regex GetBFileLineRegex();
#else
    static readonly Regex s_bFileLineRegex = new(s_bFileLineRegexString);

    static Regex GetBFileLineRegex() =>
        s_bFileLineRegex;
#endif


    public static readonly XmlReaderSettings s_linkReaderSettings = new()
    {
        ConformanceLevel = ConformanceLevel.Fragment
    };

    readonly HttpClient _oeisClient;

    public OeisDecimalExpansionDownloader(HttpClient oeisClient)
    {
        oeisClient.BaseAddress = new Uri("https://oeis.org");
        _oeisClient = oeisClient;
    }

    public async Task<OeisDecimalExpansion> DownloadAsync(OeisId id, int? maxDigits = null)
    {
        if (maxDigits < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDigits));
        }

        var idString = id.ToString();

        var (name, offset) = await DownloadSequenceInfoAsync(idString);

        var digits = await DownloadBFileTermsAsync(idString, maxDigits);

        return new OeisDecimalExpansion(id, name, BigDecimal.Create(digits, offset));
    }

    async Task<(string name, int offset)> DownloadSequenceInfoAsync(string idString)
    {
        try
        {
            using var stream = await _oeisClient.GetStreamAsync($"/search?q=id:{idString}&fmt=json");
            using var jsonDoc = await JsonDocument.ParseAsync(stream);
            var json = jsonDoc.RootElement;

            switch (json.GetProperty("count").GetInt32())
            {
                case 0:
                    throw new OeisClientException(InvalidSequence, $"No OEIS sequence was found for ID {idString}!");
                case > 1:
                    // indicates a problem with the search query
                    throw new OeisClientException(IOError,
                        $"More than a single OEIS sequence was found for ID {idString}!");
            }

            json = json.GetProperty("results")[0];

            var name = json.GetProperty("name").GetString()!;
            var offsetPair = json.GetProperty("offset").GetString()!;
            if (!Int32.TryParse(offsetPair[..offsetPair.IndexOf(',')], out var offset))
            {
                throw new OeisClientException(ParseError, $"Could not parse the sequence offset for {idString}!");
            }

            if (offset < 0)
            {
                throw new OeisClientException(InvalidSequence,
                    $"Could not interpret OEIS sequence {idString} as a decimal expansion! "
                        + "The sequence contains a negative offset.");
            }

            return (name, offset);
        }
        catch (HttpRequestException ex)
        {
            throw new OeisClientException(IOError, $"Could not retrieve sequence data for {idString} from OEIS!", ex);
        }
        catch (Exception ex) when (ex.IsSystemTextJsonException())
        {
            throw new OeisClientException(ParseError,
                $"OEIS sequence {idString} data was not returned in the expected JSON format!", ex);
        }
    }

    async Task<IReadOnlyList<byte>> DownloadBFileTermsAsync(string idString, int? maxTerms)
    {
        try
        {
            using var bFileContent =await _oeisClient.GetStreamAsync($"/{idString}/b{idString[1..]}.txt");
            using var reader = new StreamReader(bFileContent);

            var parsedTermsAsyncEnum = reader.EnumerateLinesAsync()
                .Where(static l => l != "" && l[0] != '#')
                .Select(l =>
                {
                    var parsedTerm = GetBFileLineRegex().Match(l).Groups[1].ValueSpan;

                    CheckBFileTerm(idString, parsedTerm);

                    return (byte)(parsedTerm[0] - '0');
                });

            if (maxTerms is not null)
            {
                parsedTermsAsyncEnum = parsedTermsAsyncEnum.Take((int) maxTerms);
            }

            var parsedTerms = await parsedTermsAsyncEnum.ToListAsync();

            var digits = new Fractional.DigitArray(parsedTerms.Count, 10);

            digits.Fill(parsedTerms);

            return digits;
        }
        catch (HttpRequestException ex)
        {
            throw new OeisClientException(IOError, $"Could not retrieve the b-file for {idString} from OEIS!", ex);
        }
    }

    static void CheckBFileTerm(string idString, ReadOnlySpan<char> parsedTerm)
    {
        switch (parsedTerm.Length)
        {
            case 0:
                // indicates the format of the b-file has changed
                throw new OeisClientException(ParseError, $"Could not to parse the b-file for {idString}!");

            case > 1:
                if (parsedTerm[0] == '-')
                {
                    throw new OeisClientException(InvalidSequence,
                        $"Could not interpret OEIS sequence {idString} as a decimal expansion! "
                            + "The sequence contains one or more terms that are negative.");
                }

                throw new OeisClientException(InvalidSequence,
                    $"Could not interpret OEIS sequence {idString} as a decimal expansion! "
                        + "The sequence contains one or more terms that are more than a single decimal digit.");
        }
    }
}

/// <summary>
/// Stores and retrieves OEIS fractional expansions that have been converted from decimal to dozenal.
/// </summary>
public interface IOeisDozenalExpansionStore
{
    public Task StoreAsync(OeisDozenalExpansion expansion);

    public Task<bool> ExistsAsync(OeisId id);

    public Task<OeisDozenalExpansion> RetrieveAsync(OeisId id);

    public Task<(bool success, OeisDozenalExpansion? expansion)> TryRetrieveAsync(OeisId id);
}

/// <summary>
/// Reads and writes <see cref="OeisDozenalExpansion" />s using a simple text-based format, where:<para />
///     • The first line is the name/description of the OEIS sequence.<para />
///     • The second line is the expansion terms as converted to dozenal.
/// </summary>
public static class OeisDozenalExpansionSerializer
{
    public static async Task<OeisDozenalExpansion> ReadFromAsync(OeisId id, Stream stream)
    {
        using var reader = new StreamReader(stream);

        var name = (await reader.ReadLineAsync())!;
        var dozenal = Dozenal.Parse((await reader.ReadLineAsync())!);

        Debug.Assert(await reader.ReadLineAsync() == null, "Unexpected lines in serialized expansion format!");

        return new OeisDozenalExpansion(id, name, dozenal);
    }

    public static async Task WriteToAsync(OeisDozenalExpansion expansion, Stream stream)
    {
        using var writer = new StreamWriter(stream);

        await writer.WriteLineAsync(expansion.Name);
        await writer.WriteLineAsync(expansion.Expansion.ToString());
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

    public Task<bool> ExistsAsync(OeisId id) =>
        Task.FromResult(GetFile(id).Exists);

    static async Task<OeisDozenalExpansion> RetrieveAsyncInternal(OeisId id, FileInfo file)
    {
        try
        {
            return await OeisDozenalExpansionSerializer.ReadFromAsync(id, file.OpenRead());
        }
        catch (IOException ex)
        {
            throw new OeisClientException(IOError, $"Could not retrieve a stored dozenal expansion for {id}!", ex);
        }
        catch (FormatException ex)
        {
            throw new OeisClientException(ParseError, $"Could not parse a stored dozenal expansion for {id}!", ex);
        }
    }

    public Task<OeisDozenalExpansion> RetrieveAsync(OeisId id) =>
        RetrieveAsyncInternal(id, GetFile(id));

    public async Task StoreAsync(OeisDozenalExpansion expansion)
    {
        var file = GetFile(expansion.Id);

        try
        {
            await OeisDozenalExpansionSerializer.WriteToAsync(expansion, file.OpenWrite());
        }
        catch (IOException ex)
        {
            throw new OeisClientException(IOError, $"Could not store a dozenal expansion for {expansion.Id}!", ex);
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
}

public interface IOeisDozenalExpansionService
{
    Task<OeisDozenalExpansion> RetrieveAsync(OeisId id);
}

public class OeisDozenalExpansionService : IOeisDozenalExpansionService
{
    private static readonly KeyedSemaphores<OeisId> s_locks = new();

    readonly IOeisDecimalExpansionDownloader _decimalExpansionDownloader;

    readonly IOeisDozenalExpansionStore _dozenalExpansionStore;

    public OeisDozenalExpansionService(IOeisDecimalExpansionDownloader decimalExpansionDownloader,
        IOeisDozenalExpansionStore dozenalExpansionStore)
    {
        _decimalExpansionDownloader = decimalExpansionDownloader;
        _dozenalExpansionStore = dozenalExpansionStore;
    }

    public async Task<OeisDozenalExpansion> RetrieveAsync(OeisId id)
    {
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

        var expansion = (await _decimalExpansionDownloader.DownloadAsync(id)).ConvertToDozenal();

        await _dozenalExpansionStore.StoreAsync(expansion);

        return expansion;
    }
}
