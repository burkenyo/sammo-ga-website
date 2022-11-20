using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Sammo.Oeis.Api;

interface IConfig { }

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
        public BlobsConfig? Blobs { get; init; }

        [MemberNotNullWhen(true, nameof(TenantName), nameof(ClientId), nameof(ClientSecret))]
        public bool UseClientSecretCredential =>
            (TenantName, ClientId, ClientId) is (not null, not null, not null);
    }
    
    public bool NoAzure { get; init; }
    public bool AllowDebugBuildInContainer { get; init; }
    public FileStoreConfig? FileStore { get; init; }
    public AzureConfig? Azure { get; init; }
}

static class ConfigExtensions
{
    [return: NotNull]
    public static TProp Require<TConfig, TProp>(
        this TConfig? config, Expression<Func<TConfig?, TProp?>> propertyAccessExpr) where TConfig : IConfig
    {
        TProp? prop = default;
        try
        {
            prop = propertyAccessExpr.Compile().Invoke(config);
        }
        catch (NullReferenceException) { }

        if (prop is null)
        {
            ThrowOnMissingConfigurationValue(propertyAccessExpr);
        }

        return prop;
    }

    [DoesNotReturn]
    static void ThrowOnMissingConfigurationValue<TConfig, TProp>(
        Expression<Func<TConfig?, TProp?>> propertyAccessExpr) where TConfig : IConfig
    {
        var expr = (MemberExpression) propertyAccessExpr.Body;

        Stack<string> configNodeNameParts = new();

        do
        {
            if (expr.Member.DeclaringType!.IsAssignableTo(typeof(IConfig)))
            {
                configNodeNameParts.Push(expr.Member.Name);
            }

            expr = expr.Expression as MemberExpression;
        } 
        while (expr is not null);
        
        if (typeof(TConfig).IsNested)
        {
            var configTypeFullName = typeof(TConfig).FullName!;
            var indexOfStartOfNestedTypeName = configTypeFullName.IndexOf('+') + 1;
                
            configNodeNameParts.Push(configTypeFullName[indexOfStartOfNestedTypeName..]
                .Replace('+', ':')
                .Replace("Config", ""));
        }

        var configNodeName = String.Join(':', configNodeNameParts);

        var message = typeof(TProp).IsAssignableTo(typeof(IConfig))
            ? $"Required configuration section {configNodeName} not found!"
            : $"Required configuration value {configNodeName} not found!";

        throw new InvalidOperationException(message);
    }
}