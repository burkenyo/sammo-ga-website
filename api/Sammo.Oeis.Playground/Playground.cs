// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;

namespace Sammo.Oeis.Playground;

[AttributeUsage(AttributeTargets.Method)]
class RunnerAttribute : Attribute { }

[SuppressMessage("IDE", "IDE0051:UnusedPrivateMembers")]
static partial class Playground
{
    static void Print()
    {
        Debug.WriteLine(null);
    }

    static void Print(object value)
    {
        Debug.WriteLine(value);
    }

    static void PrintExpansion<T>(IOeisFractionalExpansion<T> expansion) where T : Fractional
    {
        Print($"{expansion.Id}: {expansion.Expansion.Digits.Count} digits of “{expansion.Name}”:");
        Print(expansion.Expansion.ToString());
    }

    static async Task PlayWithOeisAsyncInternal(IOeisDecimalExpansionDownloader downloader,
        IOeisDozenalExpansionStore store,
        Dictionary<string, int> knownConstants)
    {
        var service = new OeisDozenalExpansionService(downloader, store);

        foreach (var (tag, id) in knownConstants)
        {
            var sw = Stopwatch.StartNew();

            var oeisData = await service.RetrieveAsync((OeisId)id);

            PrintExpansion(oeisData);

            Print(sw.Elapsed);
            Print();
        }
    }

    [Runner]
    static void PlayWithOeisIds()
    {
        var x = (OeisId)13;
        var y = (OeisId)47;

        Print($"{nameof(x)} = {x}, {nameof(y)} = {y}");
        Print("Interfaces:");
        foreach (var iface in typeof(OeisId).GetInterfaces())
        {
            Print(iface);
        }

        Span<OeisId> ids = stackalloc OeisId[] { x, y };

        Print("sizeof(OeisId) = " + Unsafe.SizeOf<OeisId>());

        foreach (var id in ids)
        {
            Print(id);
            Print($"Value equality?  {id == (OeisId)id.Value}");
            Print($"Hash from value? {id.GetHashCode() == id.Value.GetHashCode()}");
        }
    }

    [Runner]
    static Task PlayWithOeisLocalAsync(OeisDecimalExpansionDownloader downloader,
        OeisDozenalExpansionFileStore store, Dictionary<string, int> knownConstants)
    {
        return PlayWithOeisAsyncInternal(downloader, store, knownConstants);
    }

    [Runner]
    static Task PlayWithOeisAzureAsync(OeisDecimalExpansionDownloader downloader,
        OeisDozenalExpansionAzureBlobStore store, Dictionary<string, int> knownConstants)
    {
        return PlayWithOeisAsyncInternal(downloader, store, knownConstants);
    }

    [Runner]
    static async Task TagBlobsAsync(BlobContainerClient containerClient, Dictionary<string, int> knownConstants)
    {
        Dictionary<string, string> empty = [];

        foreach (var (tag, id) in knownConstants)
        {
            var blobName = $"{(OeisId)id}.txt";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.SetTagsAsync(empty);//new Dictionary<string, string>{{"KnownConstantTag", tag}});
            Print($"Tagged {blobName} as {tag}");
        }
    }

    [Runner]
    static async Task FindBlobsByKnownTagAsync(BlobContainerClient containerClient, Dictionary<string, int> knownConstants)
    {
        var knownTags = knownConstants.Keys;

        foreach (var tag in knownTags)
        {
            var blob = await containerClient.FindBlobsByTagsAsync($"KnownConstantTag = '{tag}'").FirstOrDefaultAsync();
            if (blob is null)
            {
                Print($"{tag} has no tagged blob.");
            }
            else
            {
                Print($"{tag} is {blob.BlobName}");
            }
        }

        await foreach (var blob in containerClient.FindBlobsByTagsAsync("\"KnownConstantTag\" > ''"))
        {
            Print($"{blob.BlobName} is tagged with {blob.Tags["KnownConstantTag"]}");
        }
    }

    [Runner]
    static async Task GetRandomExpansionsAsync(OeisDecimalExpansionDownloader downloader,
        OeisDozenalExpansionFileStore store)
    {
        var service = new OeisDozenalExpansionService(downloader, store);

        for (var i = 0; i < 1000; i++)
        {
            Print($"Random #{i + 1}");
            var sw = Stopwatch.StartNew();

            var sequence = await service.RetrieveRandomAsync(100);

            PrintExpansion(sequence);

            Print(sw.Elapsed);
            Print();
        }
    }

    [Runner]
    static void PlayWithFractionalConversions()
    {
        const double doub = 0.678968e-9;
        var dec = BigDecimal.FromDouble(doub);
        var doz = Dozenal.FromDouble(doub);
        var bin = Fractional.FromDouble(doub, 2);
        Print(Fractional.FromFractional(bin, 2));
        Print(BigDecimal.FromFractional(bin));
        Print(dec);
        Print(BigDecimal.FromFractional(doz));
        Print(Dozenal.FromFractional(bin));
        Print(Dozenal.FromFractional(dec));
        Print(doz);
    }

    [Runner]
    static void PlayWithFractionalCreate()
    {
        var cases = new[]
        {
            ("", 0, "0"),
            ("0", 1, "0"),
            ("1", 1, "1"),
            (null, 0, null),
            ("1234", 0, "0.1234"),
            ("1234", 2, "12.34"),
            ("1234", 4, "1234"),
            (null, 0, null),
            ("01234", -2, "0.0001234"),
            ("01234", 0, "0.01234"),
            ("01234", 2, "1.2340"),
            ("01234", 4, "123.40"),
            ("01234", 6, "12340"),
    };

        foreach (var (digitsString, offset, expectedString) in cases)
        {
            if (digitsString is null)
            {
                Print("                  ---");
                continue;
            }

            var digits = digitsString.Select(c => (byte)(c - '0')).ToList();

            Print($"expect: {expectedString,10}, actual {BigDecimal.Create(digits, offset),10}");
        }
    }
}
