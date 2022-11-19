#!/usr/bin/env pwsh

$keyVaultName = 'rofl-ninja-secrets'
$secretName = 'sammo-oeis-api--config'

Install-Module -Name Az.KeyVault -Scope CurrentUser

$conf = Get-Content "$PSScriptRoot/$secretName.json" |
    ConvertFrom-Json |
    ConvertTo-Json -Compress |
    ConvertTo-SecureString -AsPlainText

Connect-AzAccount
Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -SecretValue $conf