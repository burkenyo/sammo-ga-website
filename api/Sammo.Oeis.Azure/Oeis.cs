using System.Net.Mime;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Sammo.Oeis;

using static OeisClientExceptionCause;

public class OeisDozenalExpansionAzureBlobStore : IOeisDozenalExpansionStore
{
    static Lazy<BlobOpenWriteOptions> s_blobWriteOptions = new(() =>
        new BlobOpenWriteOptions
        {
            HttpHeaders = new()
            {
                ContentType = MediaTypeNames.Text.Plain
            }
        });

    readonly BlobContainerClient _client;

    public OeisDozenalExpansionAzureBlobStore(BlobContainerClient client)
    {
        _client = client;
    }

    BlobClient GetBlobClient(OeisId id) =>
        _client.GetBlobClient(id.ToString() + ".txt");

    public async Task<bool> ExistsAsyncInternal(OeisId id, BlobClient blobClient)
    {
        try
        {
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is AuthenticationFailedException)
        {
            throw new OeisClientException(IOError,
                $"Could not determine if a stored dozenal expansion exists for {id}", ex);
        }
    }

    public Task<bool> ExistsAsync(OeisId id) =>
        ExistsAsyncInternal(id, GetBlobClient(id));

    static async Task<OeisDozenalExpansion> RetrieveAsyncInternal(OeisId id, BlobClient blobClient)
    {
        try
        {
            return await OeisDozenalExpansionSerializer.ReadFromAsync(id, blobClient.OpenRead());
        }
        catch (Exception ex)
            when (ex is RequestFailedException || ex is AuthenticationFailedException || ex is IOException)
        {
            throw new OeisClientException(IOError, $"Could not retrieve a stored dozenal expansion for {id}!", ex);
        }
        catch (FormatException ex)
        {
            throw new OeisClientException(ParseError, $"Could not parse a stored dozenal expansion for {id}!", ex);
        }
    }

    public Task<OeisDozenalExpansion> RetrieveAsync(OeisId id) =>
        RetrieveAsyncInternal(id, GetBlobClient(id));

    public async Task StoreAsync(OeisDozenalExpansion expansion)
    {
        var blobClient = GetBlobClient(expansion.Id);

        try
        {
            var options = s_blobWriteOptions.Value;

            await OeisDozenalExpansionSerializer.WriteToAsync(expansion, blobClient.OpenWrite(true, options));
        }
        catch (Exception ex)
            when (ex is RequestFailedException || ex is AuthenticationFailedException || ex is IOException)
        {
            throw new OeisClientException(IOError, $"Could not store a dozenal expansion for {expansion.Id}!", ex);
        }
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
}