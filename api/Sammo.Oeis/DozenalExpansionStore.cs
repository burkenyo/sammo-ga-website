// Copyright © 2024 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

namespace Sammo.Oeis;

/// <summary>
/// Stores and retrieves OEIS fractional expansions that have been converted from decimal to dozenal.
/// </summary>
public interface IDozenalExpansionStore
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

    Task<StoredOeisExpansionInfo> StoreAsync(OeisDozenalExpansion expansion);

    Task<bool> ExistsAsync(OeisId id);

    Task<StoredOeisExpansionInfo> GetInfoAsync(OeisId id);

    Task<(bool success, StoredOeisExpansionInfo? info)> TryGetInfoAsync(OeisId id);

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
public static class DozenalExpansionSerializer
{
    public static async Task<OeisDozenalExpansion> ReadFromAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        var (id, name) = await ReadHeaderAsync(reader);
        var expansion = await ReadExpansionAsync(reader);

        return new OeisDozenalExpansion(id, name, expansion);
    }

    public static async Task<(OeisId id, string name, string preview)> ReadHeaderAndPreviewAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        var (id, name) = await ReadHeaderAsync(reader);

        return (id, name, await ReadPreviewAsync(reader));
    }

    static async Task<(OeisId id, string name)> ReadHeaderAsync(StreamReader reader)
    {
        var idString = await reader.ReadLineAsync()
                       ?? throw new IOException("Could not read the OEIS Sequence ID of the expansion!");
        var id = OeisId.Parse(idString);

        var name = await reader.ReadLineAsync()
                   ?? throw new IOException("Could not read the name of the expansion!");

        return (id, name);
    }

    /// <summary>
    /// This must always be called after <see cref="ReadHeaderAndPreviewAsync"/>
    /// </summary>
    static async Task<Dozenal> ReadExpansionAsync(StreamReader reader)
    {
        var dozenalString = await reader.ReadLineAsync()
            ?? throw new IOException("Could not read the digit sequence of the expansion!");
        var dozenal = Dozenal.Parse(dozenalString);

        if (await reader.ReadLineAsync() is not null)
        {
            throw new IOException("Unexpected lines in serialized expansion format!");
        }

        return dozenal;
    }

    static async Task<string> ReadPreviewAsync(StreamReader reader)
    {
        using var buffer = new RentedArray<char>(Fractional.DefaultMaxDigits + 1);
        var array = buffer.Array;

        var read = await reader.ReadAsync(array);

        if (read == 0)
        {
            throw new IOException("Could not read the digit sequence of the expansion!");
        }

        // constrain the IndexOf operation because rented arrays can have more elements than requested.
        var indexOfNewLine = array.AsSpan(0, Fractional.DefaultMaxDigits + 1).IndexOf('\n');

        if (indexOfNewLine == -1)
        {
            if (read <= Fractional.DefaultMaxDigits)
            {
                // the file lacked a terminal new-line
                return new String(array, 0, read);
            }

            // the file has more digits
            array[Fractional.DefaultMaxDigits] = '…';
            return new String(array, 0, Fractional.DefaultMaxDigits + 1);
        }

        if (indexOfNewLine != read - 1)
        {
            throw new IOException("Unexpected lines in serialized expansion format!");
        }

        return new String(array, 0, indexOfNewLine);
    }

    public static async Task WriteToAsync(OeisDozenalExpansion expansion, Stream stream)
    {
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.NewLine = "\n";

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
public static class BadOeisSequenceListUtil
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
        await using var writer = new StreamWriter(stream,  leaveOpen: true);
        writer.NewLine = "\n";

        await writer.WriteAsync(id.ToString());
        await writer.WriteAsync(": ");
        await writer.WriteLineAsync(reason);
        await writer.FlushAsync();
    }
}

public class DozenalExpansionFileStore : IDozenalExpansionStore
{
    readonly DirectoryInfo _directory;

    public DozenalExpansionFileStore(DirectoryInfo directory)
    {
        _directory = directory;
    }

    FileInfo GetFile(OeisId id) =>
        new FileInfo(Path.Combine(_directory.FullName, id + ".txt"));

    async Task<StoredOeisExpansionInfo> GetInfoAsyncInternal(OeisId id, FileInfo file)
    {
        try
        {
            await using var stream = file.OpenRead();
            var (readId, name, preview) = await DozenalExpansionSerializer.ReadHeaderAndPreviewAsync(stream);

            if (readId != id)
            {
                throw IDozenalExpansionStore.Errors.IO.Retrieve(id);
            }

            return new StoredOeisExpansionInfo(id, name, Dozenal.Radix, preview, new Uri(file.FullName));
        }
        catch (IOException ex)
        {
            throw IDozenalExpansionStore.Errors.IO.Retrieve(id, ex);
        }
    }

    public Task<StoredOeisExpansionInfo> GetInfoAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            throw IDozenalExpansionStore.Errors.NotFound(id);
        }

        return GetInfoAsyncInternal(id, file);
    }

    public async Task<(bool, StoredOeisExpansionInfo?)> TryGetInfoAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            return (false, null);
        }

        return (true, await GetInfoAsyncInternal(id, file));
    }


    public Task<bool> ExistsAsync(OeisId id) =>
        Task.FromResult(GetFile(id).Exists);

    static async Task<OeisDozenalExpansion> RetrieveAsyncInternal(OeisId id, FileInfo file)
    {
        try
        {
            await using var stream = file.OpenRead();
            var expansion = await DozenalExpansionSerializer.ReadFromAsync(stream);

            if (expansion.Id != id)
            {
                throw IDozenalExpansionStore.Errors.IO.Retrieve(id);
            }

            return expansion;
        }
        catch (IOException ex)
        {
            throw IDozenalExpansionStore.Errors.IO.Retrieve(id, ex);
        }
        catch (FormatException ex)
        {
            throw IDozenalExpansionStore.Errors.Parse(id, ex);
        }
    }

    public Task<OeisDozenalExpansion> RetrieveAsync(OeisId id)
    {
        var file = GetFile(id);

        if (!file.Exists)
        {
            throw IDozenalExpansionStore.Errors.NotFound(id);
        }

        return RetrieveAsyncInternal(id, file);
    }

    public async Task<StoredOeisExpansionInfo> StoreAsync(OeisDozenalExpansion expansion)
    {
        var file = GetFile(expansion.Id);

        try
        {
            await using var stream = file.OpenWrite();
            await DozenalExpansionSerializer.WriteToAsync(expansion, stream);

            return new StoredOeisExpansionInfo(expansion.Id, expansion.Name, Dozenal.Radix,
                expansion.Expansion.ToString(Fractional.DefaultMaxDigits), new Uri(file.FullName));
        }
        catch (IOException ex)
        {
            throw IDozenalExpansionStore.Errors.IO.Store(expansion.Id, ex);
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
            await using var stream = File.Open(GetBadSequenceListPath(), FileMode.OpenOrCreate, FileAccess.Read);

            return await BadOeisSequenceListUtil.BadSequenceListContainsAsync(stream, id);
        }
        catch (IOException ex)
        {
            throw IDozenalExpansionStore.Errors.BadSequenceList.Exists(id, ex);
        }
    }

    public async Task AddToBadSequenceListAsync(OeisId id, string message)
    {
        try
        {
            await using var stream = File.Open(GetBadSequenceListPath(), FileMode.OpenOrCreate);

            if (await BadOeisSequenceListUtil.BadSequenceListContainsAsync(stream, id) is (true, _))
            {
                return;
            }

            // append the new item
            stream.Seek(0, SeekOrigin.End);
            await BadOeisSequenceListUtil.AddToBadSequenceList(stream, id, message);
        }
        catch (IOException ex)
        {
            throw IDozenalExpansionStore.Errors.BadSequenceList.Add(id, ex);
        }
    }
}
