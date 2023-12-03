// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Sammo.Oeis.Api;

/// <summary>
/// Marker interface indicating that a class represents a section or sub-section of app configuration
/// </summary>
interface IConfig { }

/// <summary>
/// Strongly-typed configuration
/// </summary>
/// <remarks>
/// Class must be structured as follows:<para />
///     • Classes representing sub-sections should be nested in the class representing their parent section.<para />
///     • Classes representing sub-sections should be named as the name of the section they present + “Config”.<para />
///     • This class classes and all classes representing sub-sections must “implement” IConfig.<para />
///     • Properties should be public and read-only (for example, when using expression-bodied properties)
///         or have init-only setters;
///     • Properties representing sub-sections should be non-nullable and
///         initialized using the default constructor.<para />
///     • Properties representing scalar config values should be nullable or
///         initialized to an appropriate default.<para />
///     • Properties representing config value collections should be typed as read-only collection interfaces
///         (note: ConfigurationBinder limits what types it will use for the keys of sets and dictionaries).<para />
///     • Properties representing config value collections should be initialized to empty.<para />
/// Following these guidelines ensure that the config binds correctly and that the Require() extension method
/// will return helpful error messages in the case of missing config values.
/// </remarks>
class Config : IConfig
{
    public class FileStoreConfig : IConfig
    {
        public string? DataDirectory { get; init; }
    }

    public class AzureConfig : IConfig
    {
        public class BlobsConfig : IConfig
        {
            public string? AccountName { get; init; }
            public string? ContainerName { get; init; }
        }

        public string? TenantName { get; init; }
        public string? ClientId { get; init; }
        public string? ClientSecret { get; init; }
        public string? KeyVaultName { get; init; }
        public BlobsConfig Blobs { get; init; } = new();

        [MemberNotNullWhen(true, nameof(TenantName), nameof(ClientId), nameof(ClientSecret))]
        public bool UseClientSecretCredential =>
            (TenantName, ClientId, ClientId) is (not null, not null, not null);
    }

    public class CorsConfig : IConfig
    {
        public bool AllowAnyOrigin { get; init; }
        public IReadOnlyList<Uri> AllowedOrigins { get; init; } = [];

        public bool UseCors =>
            AllowAnyOrigin || AllowedOrigins.Any();

        public string[] GetAllowedOrigins() =>
            AllowedOrigins
                // GetLeftPart(UriPartial.Authority) returns the scheme and authority with no trailing slash
                .Select(o => o.GetLeftPart(UriPartial.Authority))
                .ToArray();
    }

    public bool NoAzure { get; init; }
    public bool AllowDebugBuildInContainer { get; init; }
    public FileStoreConfig FileStore { get; init; } = new();
    public AzureConfig Azure { get; init; } = new();
    public CorsConfig Cors { get; init; } = new();
}

static class ConfigExtensions
{
    /// <summary>
    /// Invokes the provided Func and returns the value as long as it is none of the following:<para/>
    ///     • null<para/>
    ///     • an instance of value type equal to the default value for that type;
    ///         for example, a numeric value equal to 0<para/>
    ///     • an empty collection<para/>
    /// In any of the above cases, an exception is thrown detailing which configuration value is missing.
    /// </summary>
    [return: NotNull]
    public static TProp Require<TConfig, TProp>(this TConfig config, Func<TConfig, TProp> propertyAccessor,
        [CallerArgumentExpression(nameof(propertyAccessor))] string expression = null!) where TConfig : IConfig
    {
        var prop = propertyAccessor(config);

        if (prop is null or "" or false or 0 or 0L or 0D or 0M
            || (prop is IEquatable<TProp> equatable && equatable.Equals(default))
            || (prop is IEnumerable enumerable && !enumerable.Cast<object>().Any()))
        {
            ThrowOnMissingConfigurationValue<TConfig, TProp>(expression);
        }

        return prop;
    }

    [DoesNotReturn]
    static void ThrowOnMissingConfigurationValue<TConfig, TProp>(string expression) where TConfig : IConfig
    {
        StringBuilder configNodeNameBuilder = new(capacity: 40);

        // If we’re in one of the nested config types, its name is part of the configuration node name.
        if (typeof(TConfig).IsNested)
        {
            var configTypeFullName = typeof(TConfig).FullName!;
            var indexOfStartOfNestedTypeName = configTypeFullName.IndexOf('+') + 1;

            configNodeNameBuilder.Append(configTypeFullName.AsSpan()[indexOfStartOfNestedTypeName..])
                .Replace("Config", null)
                .Replace('+', ':')
                .Append(':');
        }

        // Get the rest of the configuration node name from the expression.
        configNodeNameBuilder.Append(expression.AsSpan()[(expression.IndexOf('.') + 1)..])
            .Replace("?", null)
            .Replace('.', ':');

        string? collectionTypeName = null;

        if (typeof(TProp) != typeof(String)
            && typeof(TProp).GetInterface(typeof(IEnumerable<>).Name) is { } iEnumOfTType)
        {
            collectionTypeName = iEnumOfTType.GetGenericArguments()[0].Name;
        }

        var message = collectionTypeName is not null
            ? $"Required collection of {collectionTypeName} configuration values “{configNodeNameBuilder}” not set!"
            : $"Required {typeof(TProp).Name} configuration value “{configNodeNameBuilder}” not set!";

        throw new InvalidOperationException(message);
    }
}

/// <summary>
/// Supports adding an instance of a class implementing <see cref="IConfig"/> directly
/// to the dependency injection container without needing <see cref="IOptions{TOptions}"/>.
/// </summary>
static class ServiceCollectionConfigExtensions
{
    public static IServiceCollection AddConfig<TConfig>(this IServiceCollection services)
        where TConfig : class, IConfig, new()
    {
        services.TryAddScoped<TConfig>(provider =>
            provider.GetRequiredService<IConfiguration>().Get<TConfig>() ?? new());

        return services;
    }

}
