using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace Sammo.Oeis;

public class OeisDozenalExpansionAzureBlobStore : IOeisDozenalExpansionStore
{
    static Lazy<BlobOpenWriteOptions> s_expansionWriteOptions = new(() =>
        new BlobOpenWriteOptions
        {
            HttpHeaders = new()
            {
                ContentType = "text/plain; charset=utf-8"
            }
        });

    // since every invocation of CreateIfNotExists will need options, cache this
    static Lazy<AppendBlobCreateOptions> s_badSequenceListCreateOptions = new(() =>
        new AppendBlobCreateOptions
        {
            HttpHeaders = new()
            {
                ContentType = "text/csv; charset=utf-8"
            }
        });

    readonly BlobContainerClient _client;

    public OeisDozenalExpansionAzureBlobStore(BlobContainerClient client)
    {
        _client = client;
    }

    BlobClient GetBlobClient(OeisId id) =>
        _client.GetBlobClient(id.ToString() + ".txt");

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

    public async Task<Uri> GetUriAsync(OeisId id)
    {
        var blobClient = GetBlobClient(id);

        if (!await ExistsAsyncInternal(id, blobClient))
        {
            throw IOeisDozenalExpansionStore.Errors.NotFound(id);
        }

        return blobClient.Uri;
    }

    public async Task<(bool, Uri?)> TryGetUriAsync(OeisId id)
    {
        var blobClient = GetBlobClient(id);

        if (!await ExistsAsyncInternal(id, blobClient))
        {
            return (false, null);
        }

        return (true, blobClient.Uri);
    }

    static async Task<OeisDozenalExpansion> RetrieveAsyncInternal(OeisId id, BlobClient blobClient)
    {
        try
        {
            var expansion = await OeisDozenalExpansionSerializer.ReadFromAsync(blobClient.OpenRead());

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

    public async Task<Uri> StoreAsync(OeisDozenalExpansion expansion)
    {
        var blobClient = GetBlobClient(expansion.Id);

        try
        {
            var options = s_expansionWriteOptions.Value;

            await OeisDozenalExpansionSerializer.WriteToAsync(expansion, blobClient.OpenWrite(true, options));

            return blobClient.Uri;
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
        var badSequenceListClient = _client.GetAppendBlobClient("bad.csv");

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

            Stream stream;

            // explicit using block so stream is closed before any writes
            using (stream = await badSequenceListClient.OpenReadAsync())
            {
                if (await OeisBadSequenceListUtil.BadSequenceListContainsAsync(stream, id) is (true, _))
                {
                    return;
                }
            }

            stream = await badSequenceListClient.OpenWriteAsync(false);

            await OeisBadSequenceListUtil.AddToBadSequenceList(stream, id, reason);

        }
        catch (Exception ex) when (ShouldWrap(ex))
        {
            throw IOeisDozenalExpansionStore.Errors.BadSequenceList.Add(id, ex);
        }
    }

    static bool ShouldWrap(Exception ex) =>
        ex is RequestFailedException || ex is AuthenticationFailedException || ex is IOException;
}