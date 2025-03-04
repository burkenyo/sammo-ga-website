// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Text;

namespace Sammo.Oeis.Api;

class LocalTestingDozenalExpansionService : IDozenalExpansionService
{
    public const string UriPath = "/data";

    readonly DirectoryInfo _dataDir;
    readonly HttpRequest _httpRequest;
    readonly DozenalExpansionService _service;

    public LocalTestingDozenalExpansionService(IDecimalExpansionDownloader decimalExpansionDownloader,
        IDozenalExpansionStore dozenalExpansionStore, DirectoryInfo dataDir,
        IHttpContextAccessor httpContextAccessor, ILogger<DozenalExpansionService> logger)
    {
        _dataDir = dataDir;
        _httpRequest = httpContextAccessor.HttpContext!.Request;
        _service = new DozenalExpansionService(decimalExpansionDownloader, dozenalExpansionStore, logger);
    }

    public async Task<StoredOeisExpansionInfo> GetInfoAsync(OeisId id) =>
        RepointUri(await _service.GetInfoAsync(id));

    public Task<OeisDozenalExpansion> RetrieveAsync(OeisId id) =>
        _service.RetrieveAsync(id);

    public async Task<StoredOeisExpansionInfo> GetInfoForRandomAsync(int maxTries = 3) =>
        RepointUri( await _service.GetInfoForRandomAsync(maxTries));

    public Task<OeisDozenalExpansion> RetrieveRandomAsync(int maxTries = 3) =>
        _service.RetrieveRandomAsync(maxTries);

    StoredOeisExpansionInfo RepointUri(StoredOeisExpansionInfo info)
    {
        var builder = new StringBuilder(120)
            .Append(_httpRequest.Scheme)
            .Append("://")
            .Append(_httpRequest.Host.ToString())
            .Append(UriPath)
            .Append('/')
            .Append(new Uri(_dataDir.FullName + '/').MakeRelativeUri(info.Uri));

        return new StoredOeisExpansionInfo(
            info.Id, info.Name, info.Radix, info.Preview, new Uri(builder.ToString()));
    }
}
