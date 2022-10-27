using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Azure.Storage;
using Azure.Storage.Blobs;
using Sammo.Oeis;

namespace Sammo.Oeis.Playground;

static class Program
{
    readonly static Action<object?> p = Console.WriteLine;

    static void Main(string[] args)
    {
        static string? GetDisplayName(MethodInfo m) =>
            m.Name.EndsWith("Async")
                ? m.Name[..^5]
                : m.Name.EndsWith("Internal")
                    ? null
                    : m.Name;

        var toRun = typeof(Program)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name != nameof(Main))
            .IntersectBy(args, m => GetDisplayName(m));

        var ranSomething = false;
        foreach(var method in toRun)
        {
            p($"-----\nRunning {GetDisplayName(method)}...\n-----\n");
            if (method.ReturnType.IsAssignableTo(typeof(Task)))
            {
                method.CreateDelegate<Func<Task>>().Invoke().Wait();
            }
            else
            {
                method.CreateDelegate<Action>().Invoke();
            }

            ranSomething = true;
        }

        if (!ranSomething)
        {
            p("Nothing selected to run!");
        }
    }

    static async Task PlayWithOeisAsyncInternal(IOeisDozenalExpansionStore store)
    {
        Dictionary<string, int> oeisIds;

        Environment.CurrentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

        using (var file = File.OpenRead("oeis.json"))
        {
            var doc = await JsonDocument.ParseAsync(file);
            oeisIds = doc.RootElement
                .EnumerateObject()
                    .ToDictionary(
                        static p => p.Name,
                        static p => p.Value.GetInt32()
                    );
        }

        using var _oeisClient = new HttpClient();
        var downloader = new OeisDecimalExpansionDownloader(_oeisClient);

        var service = new OeisDozenalExpansionService(downloader, store);

        foreach (var (tag, id) in oeisIds)
        {
            var sw = Stopwatch.StartNew();

            var oeisData = await service.RetrieveAsync((OeisId)id);

            p($"{oeisData.Expansion.Digits.Count} digits of {oeisData.Name}:");
            p(oeisData.Expansion.ToString()[..300] + '…');

            p(sw.Elapsed);
            p(null);
        }
    }

    static Task PlayWithOeisLocalAsync()
    {
        DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);

        while (true)
        {
            var data = directory.EnumerateDirectories("data").SingleOrDefault();

            if (data is not null)
            {
                directory = data;
                break;
            }

            directory = directory.Parent ?? throw new DirectoryNotFoundException();
        }

        var store = new OeisDozenalExpansionFileStore(directory);

        return PlayWithOeisAsyncInternal(store);
    }

    static Task PlayWithOeisAzureAsync()
    {
        var accountName = "roflninjastorage";
        var containerName = "sammo-ga";

        var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        var credential = new StorageSharedKeyCredential(accountName, accountKey);

        var containerClient = new BlobContainerClient(containerUri, credential);

        var store = new OeisDozenalExpansionAzureBlobStore(containerClient);

        return PlayWithOeisAsyncInternal(store);
    }
}

static class StopwatchExtensions
{
    public static TimeSpan GetElapsedAndRestart(this Stopwatch sw)
    {
        var elapsed = sw.Elapsed;
        sw.Restart();
        return elapsed;
    }
}