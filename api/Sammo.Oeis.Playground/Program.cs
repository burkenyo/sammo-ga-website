using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Sammo.Oeis.Playground;

static class Program
{
    static void Main(string[] args)
    {
        Trace.Listeners.Add(new ConsoleTraceListener());

        static string? GetDisplayName(MethodInfo m) =>
            m.Name.EndsWith("Async")
                ? m.Name[..^5]
                : m.Name;

        var runners = typeof(Playground)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.IsDefined(typeof(RunnerAttribute)))
            .ToList();

        var toRun = args.Contains("*")
            ? runners
            : runners
                .IntersectBy(args, m => GetDisplayName(m))
                .ToList();

        if (toRun.Any())
        {
            var services = new ServiceCollection()
                .AddSingleton(p => GetKnownConstantsAsync().Result)
                .AddSingleton(p => GetDataDirectory())
                .AddSingleton(p => GetContainerClient())
                .AddTransient<HttpClient>()
                .AddSingleton<OeisDecimalExpansionDownloader>()
                .AddSingleton<OeisDozenalExpansionFileStore>()
                .AddSingleton<OeisDozenalExpansionAzureBlobStore>()
                .BuildServiceProvider();

            foreach (var method in toRun)
            {
                Console.WriteLine($"-----\nRunning {GetDisplayName(method)}...\n-----\n");

                var @params = method.GetParameters()
                    .Select(p => services.GetService(p.ParameterType))
                    .ToArray();

                if (method.ReturnType.IsAssignableTo(typeof(Task)))
                {
                    ((Task)method.Invoke(null, @params)!).Wait();
                }
                else
                {
                    method.Invoke(null, @params);
                }
            }
        }
        else
        {
            var options = String.Join(", ", runners.Select(m => GetDisplayName(m)));

            Console.WriteLine($"Nothing selected to run! Options are {options}.\nUse '*' to run all (may need to quote in shell).");
        }
    }

    static async Task<Dictionary<string, int>> GetKnownConstantsAsync()
    {
        string path = Path.Join(Path.GetDirectoryName(typeof(Program).Assembly.Location), "oeis.json");

        using var file = File.OpenRead(path);
        var doc = await JsonDocument.ParseAsync(file);

        var knownConstants = doc.RootElement
            .EnumerateObject()
            .ToDictionary(
                static p => p.Name,
                static p => p.Value.GetProperty("id").GetInt32()
            );

        return knownConstants;
    }

    static DirectoryInfo GetDataDirectory()
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

        return directory;
    }

    static BlobContainerClient GetContainerClient()
    {
        const string accountName = "roflninjastorage";
        const string containerName = "sammo-ga";

        var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");

        return new BlobContainerClient(containerUri, new DefaultAzureCredential());
    }
}