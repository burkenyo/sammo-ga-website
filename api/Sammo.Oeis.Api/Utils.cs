using System.Diagnostics.CodeAnalysis;

namespace Sammo.Oeis.Api;

static class LoggerExtension
{
    [SuppressMessage("Usage", "CA2254:UseStaticFormatStrings", Justification = "Format string is built dynamically.")]
    public static void Log(
        this ILogger logger, LogLevel logLevel, string message, params (string label, object? value)[] details)
    {
        var detailsToInclude = details.Where(static p => p.value is not null);
        var detailsFormatString = String.Join("; ", detailsToInclude.Select(static d => d.label));
        var detailsValues = detailsToInclude.Select(static d => d.value).ToArray();

        logger.Log(logLevel, message + detailsFormatString, detailsValues);
    }
}