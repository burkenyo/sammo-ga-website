// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using Microsoft.Extensions.Logging;

namespace Sammo.Oeis;

public interface IDozenalExpansionService
{
    Task<StoredOeisExpansionInfo> GetInfoAsync(OeisId id);

    Task<OeisDozenalExpansion> RetrieveAsync(OeisId id);

    Task<StoredOeisExpansionInfo> GetInfoForRandomAsync(int maxTries = 3);

    Task<OeisDozenalExpansion> RetrieveRandomAsync(int maxTries = 3);
}

public class DozenalExpansionService : IDozenalExpansionService
{
    private static readonly KeyedSemaphores<OeisId> s_locks = new();

    readonly IDecimalExpansionDownloader _decimalExpansionDownloader;

    readonly IDozenalExpansionStore _dozenalExpansionStore;

    readonly ILogger? _logger;

    public DozenalExpansionService(
        IDecimalExpansionDownloader decimalExpansionDownloader,
        IDozenalExpansionStore dozenalExpansionStore,
        ILogger<DozenalExpansionService>? logger = null)
    {
        _decimalExpansionDownloader = decimalExpansionDownloader;
        _dozenalExpansionStore = dozenalExpansionStore;
        _logger = logger;
    }

    public async Task<StoredOeisExpansionInfo> GetInfoAsync(OeisId id)
    {
        if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, { } message))
        {
            throw OeisClientException.InvalidSequence(message, id);
        }

        // Try to find an expansion that was already downloaded and converted.
        if (await _dozenalExpansionStore.TryGetInfoAsync(id) is (true, { } info))
        {
            return info;
        }

        using var semaphore = s_locks.Borrow(id);
        await semaphore.WaitAsync();

        // Try again to find the expansion now that we have the semaphore.
        // This is predicated on the assumption that it’s cheaper to try to find the expansion again
        // if another request just downloaded and converted it than doing that here
        // and overwriting any stored result.
        if (await _dozenalExpansionStore.TryGetInfoAsync(id) is (true, { } info2))
        {
            return info2;
        }

        try
        {
            _logger?.LogInformation("{id} not found in store, attempting to download.", id);

            var expansion = (await _decimalExpansionDownloader.DownloadAsync(id)).ConvertToDozenal();

            info = await _dozenalExpansionStore.StoreAsync(expansion);

            return info;
        }
        catch (OeisClientException ex) when (ex.Cause == ClientExceptionCause.InvalidSequence)
        {
            _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

            await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

            throw;
        }
    }

    public async Task<OeisDozenalExpansion> RetrieveAsync(OeisId id)
    {
        if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, { } message))
        {
            throw OeisClientException.InvalidSequence(message, id);
        }

        // Try to find an expansion that was already downloaded and converted.
        if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, { } existingExpansion))
        {
            return existingExpansion;
        }

        using var semaphore = s_locks.Borrow(id);
        await semaphore.WaitAsync();

        // Try again to find the expansion now that we have the semaphore.
        // This is predicated on the assumption that it’s cheaper to try to find the expansion again
        // if another request just downloaded and converted it than doing that here
        // and overwriting any stored result.
        if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, { } existingExpansion2))
        {
            return existingExpansion2;
        }

        try
        {
            _logger?.LogInformation("{id} not found in store, attempting to download.", id);

            var expansion = (await _decimalExpansionDownloader.DownloadAsync(id)).ConvertToDozenal();

            await _dozenalExpansionStore.StoreAsync(expansion);

            return expansion;
        }
        catch (OeisClientException ex) when (ex.Cause == ClientExceptionCause.InvalidSequence)
        {
            _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

            await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

            throw;
        }
    }

    public async Task<StoredOeisExpansionInfo> GetInfoForRandomAsync(int maxTries = 3)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTries, 1);

        List<OeisClientException>? failures = null;

        for (var i = 0; i < maxTries; i++)
        {
            OeisSequence sequence;
            try
            {
                sequence = await _decimalExpansionDownloader.GetRandomSequence();
            }
            catch (OeisClientException ex) when (ex.Cause == ClientExceptionCause.InvalidSequence)
            {
                (failures ??= []).Add(ex);

                _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

                continue;
            }

            var id = sequence.Id;

            if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, { } message))
            {
                (failures ??= []).Add(OeisClientException.InvalidSequence(message, id));

                continue;
            }

            // Try to find an expansion that was already downloaded and converted.
            if (await _dozenalExpansionStore.TryGetInfoAsync(id) is (true, { } info))
            {
                return info;
            }

            using var semaphore = s_locks.Borrow(id);
            await semaphore.WaitAsync();

            // Try again to find the expansion now that we have the semaphore.
            // This is predicated on the assumption that it’s cheaper to try to find the expansion again
            // if another request just downloaded and converted it than doing that here
            // and overwriting any stored result.
            if (await _dozenalExpansionStore.TryGetInfoAsync(id) is (true, { } info2))
            {
                return info2;
            }

            try
            {
                _logger?.LogInformation("{id} not found in store, attempting to hydrate.", id);

                var expansion = (await _decimalExpansionDownloader.HydrateAsync(sequence)).ConvertToDozenal();

                info = await _dozenalExpansionStore.StoreAsync(expansion);

                return info;
            }
            catch (OeisClientException ex) when (ex.Cause == ClientExceptionCause.InvalidSequence)
            {
                (failures ??= []).Add(ex);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);
            }
        }

        throw OeisClientException.IOError($"Could not retrieve a valid sequence after {maxTries} tries!",
            new AggregateException(failures!));
    }

    public async Task<OeisDozenalExpansion> RetrieveRandomAsync(int maxTries = 3)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTries, 1);

        List<OeisClientException>? failures = null;

        for (var i = 0; i < maxTries; i++)
        {
            OeisSequence sequence;
            try
            {
                sequence = await _decimalExpansionDownloader.GetRandomSequence();

            }
            catch (OeisClientException ex) when (ex.Cause == ClientExceptionCause.InvalidSequence)
            {
                (failures ??= []).Add(ex);

                _logger?.LogInformation("Adding {id} to the bad sequence list. Reason: “{message}”", ex.Id, ex.Message);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);

                continue;
            }

            var id = sequence.Id;

            if (await _dozenalExpansionStore.BadSequenceListContainsAsync(id) is (true, { } message))
            {
                (failures ??= []).Add(OeisClientException.InvalidSequence(message, id));

                continue;
            }

            // Try to find an expansion that was already downloaded and converted.
            if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, { } existingExpansion))
            {
                return existingExpansion;
            }

            using var semaphore = s_locks.Borrow(id);
            await semaphore.WaitAsync();

            // Try again to find the expansion now that we have the semaphore.
            // This is predicated on the assumption that it’s cheaper to try to find the expansion again
            // if another request just downloaded and converted it than doing that here
            // and overwriting any stored result.
            if (await _dozenalExpansionStore.TryRetrieveAsync(id) is (true, { } existingExpansion2))
            {
                return existingExpansion2;
            }

            try
            {
                _logger?.LogInformation("{id} not found in store, attempting to hydrate.", id);

                var expansion = (await _decimalExpansionDownloader.HydrateAsync(sequence)).ConvertToDozenal();

                await _dozenalExpansionStore.StoreAsync(expansion);

                return expansion;
            }
            catch (OeisClientException ex) when (ex.Cause == ClientExceptionCause.InvalidSequence)
            {
                (failures ??= []).Add(ex);

                await _dozenalExpansionStore.AddToBadSequenceListAsync((OeisId) ex.Id!, ex.Message);
            }
        }

        throw OeisClientException.IOError($"Could not retrieve a valid sequence after {maxTries} tries!",
            new AggregateException(failures!));
    }
}
