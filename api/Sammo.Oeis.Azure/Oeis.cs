// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace Sammo.Oeis;

public class OeisDozenalExpansionAzureBlobStore : IOeisDozenalExpansionStore
{
    /// <summary>
    /// since every invocation of CreateIfNotExistsAsync will need options, cache this
    /// </summary>
    static Lazy<BlobOpenWriteOptions> s_expansionWriteOptions = new(() =>
        new BlobOpenWriteOptions
        {
            HttpHeaders = new()
            {
                ContentType = "text/plain; charset=utf-8"
            }
        });

    /// <summary>
    /// since every invocation of OpenWrite will need options, cache this
    /// </summary>
    static Lazy<AppendBlobCreateOptions> s_badSequenceListCreateOptions = new(() =>
        new AppendBlobCreateOptions
        {
            HttpHeaders = new()
            {
                ContentType = "text/plain; charset=utf-8"
            }
        });

    readonly BlobContainerClient _client;

    public OeisDozenalExpansionAzureBlobStore(BlobContainerClient client)
    {
        _client = client;
    }

    BlobClient GetBlobClient(OeisId id) =>
        _client.GetBlobClient(id + ".txt");

    static async Task<bool> ExistsAsyncInternal(OeisId id, BlobClient blobClient)
    {
        try
        {
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw OeisClientException.IOError(
                $"Could not determine if a stored dozenal expansion exists for {id}", id, ex);
        }
    }

    public Task<bool> ExistsAsync(OeisId id) =>
        ExistsAsyncInternal(id, GetBlobClient(id));

    async Task<StoredOeisExpansionInfo> GetInfoAsyncInternal(OeisId id, BlobClient blobClient)
    {
        try
        {
            // optimize for only pulling the header from the blob
            using var stream = blobClient.OpenRead(bufferSize: 1024);
            var (readId, name, preview) = await OeisDozenalExpansionSerializer.ReadHeaderAndPreviewAsync(stream);

            if (readId != id)
            {
                throw IOeisDozenalExpansionStore.Errors.IO.Retrieve(id);
            }

            return new StoredOeisExpansionInfo(id, name, Dozenal.Radix, preview, blobClient.Uri);
        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw IOeisDozenalExpansionStore.Errors.IO.Retrieve(id, ex);
        }
    }

    public async Task<StoredOeisExpansionInfo> GetInfoAsync(OeisId id)
    {
        var blobClient = GetBlobClient(id);

        if (!await ExistsAsyncInternal(id, blobClient))
        {
            throw IOeisDozenalExpansionStore.Errors.NotFound(id);
        }

        return await GetInfoAsyncInternal(id, blobClient);
    }

    public async Task<(bool, StoredOeisExpansionInfo?)> TryGetInfoAsync(OeisId id)
    {
        var blobClient = GetBlobClient(id);

        if (!await ExistsAsyncInternal(id, blobClient))
        {
            return (false, null);
        }

        return (true, await GetInfoAsyncInternal(id, blobClient));
    }

    static async Task<OeisDozenalExpansion> RetrieveAsyncInternal(OeisId id, BlobClient blobClient)
    {
        try
        {
            // optimize for reading the whole expansion (131_072 is 2^17)
            using var stream = await blobClient.OpenReadAsync(bufferSize: 131_072);
            var expansion = await OeisDozenalExpansionSerializer.ReadFromAsync(stream);

            if (expansion.Id != id)
            {
                throw IOeisDozenalExpansionStore.Errors.IO.Retrieve(id);
            }

            return expansion;
        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw IOeisDozenalExpansionStore.Errors.IO.Retrieve(id, ex);
        }
        catch (FormatException ex)
        {
            throw IOeisDozenalExpansionStore.Errors.Parse(id, ex);
        }
    }

    public async Task<StoredOeisExpansionInfo> StoreAsync(OeisDozenalExpansion expansion)
    {
        var blobClient = GetBlobClient(expansion.Id);

        try
        {
            var options = s_expansionWriteOptions.Value;

            using var stream = await blobClient.OpenWriteAsync(true, options);
            await OeisDozenalExpansionSerializer.WriteToAsync(expansion, stream);

            return new StoredOeisExpansionInfo(expansion.Id, expansion.Name, Dozenal.Radix,
                expansion.Expansion.ToString(maxDigits: Fractional.DefaultMaxDigits), blobClient.Uri);
        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw IOeisDozenalExpansionStore.Errors.IO.Store(expansion.Id, ex);
        }
    }

    public async Task<OeisDozenalExpansion> RetrieveAsync(OeisId id)
    {
        var blobClient = GetBlobClient(id);

        if (!await ExistsAsyncInternal(id, blobClient))
        {
            throw IOeisDozenalExpansionStore.Errors.NotFound(id);
        }

        return await RetrieveAsyncInternal(id, blobClient);
    }

    public async Task<(bool success, OeisDozenalExpansion? expansion)> TryRetrieveAsync(OeisId id)
    {
        var blobClient = GetBlobClient(id);

        if (!await ExistsAsyncInternal(id, blobClient))
        {
            return (false, null);
        }

        return (true, await RetrieveAsyncInternal(id, blobClient));
    }

    async Task<AppendBlobClient> GetBadSequenceListClient()
    {
        var badSequenceListClient = _client.GetAppendBlobClient("bad.txt");

        await badSequenceListClient.CreateIfNotExistsAsync(s_badSequenceListCreateOptions.Value);

        return badSequenceListClient;
    }

    public async Task<(bool result, string? reason)> BadSequenceListContainsAsync(OeisId id)
    {
        try
        {
            var badSequenceListClient = await GetBadSequenceListClient();

            using var stream = await badSequenceListClient.OpenReadAsync();

            return await OeisBadSequenceListUtil.BadSequenceListContainsAsync(stream, id);
        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw IOeisDozenalExpansionStore.Errors.BadSequenceList.Exists(id, ex);
        }
    }

    public async Task AddToBadSequenceListAsync(OeisId id, string reason)
    {
        try
        {
            var badSequenceListClient = await GetBadSequenceListClient();

            // explicit using blocks so stream is closed before any writes
            using (var stream = await badSequenceListClient.OpenReadAsync())
            {
                if (await OeisBadSequenceListUtil.BadSequenceListContainsAsync(stream, id) is (true, _))
                {
                    return;
                }
            }

            // use “overwrite: false” to stipulate appending
            using (var stream = await badSequenceListClient.OpenWriteAsync(overwrite: false))
            {
                await OeisBadSequenceListUtil.AddToBadSequenceList(stream, id, reason);
            }

        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw IOeisDozenalExpansionStore.Errors.BadSequenceList.Add(id, ex);
        }
    }

    static bool ShouldWrap(Exception ex) =>
        ex is RequestFailedException || ex is AuthenticationFailedException || ex is IOException;
}
