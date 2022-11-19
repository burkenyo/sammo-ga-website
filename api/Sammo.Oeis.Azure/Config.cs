using System.Text;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Sammo.Oeis;

public static class AzureKeyVaultConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureKeyVaultJsonSecret(
        this IConfigurationBuilder builder, string vaultName, string secretName, TokenCredential credential)
    {
        var vaultUri = new Uri($"https://{vaultName}.vault.azure.net");
        var client = new SecretClient(vaultUri, credential);

        // It’s a bit hacky to go in and out of streams, but that’s what I have to do
        var getConfigResult = client.GetSecret(secretName);
        var configString = getConfigResult.Value.Value;

        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configString)));

        return builder;
    }
}

