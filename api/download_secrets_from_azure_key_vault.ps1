#!/usr/bin/env pwsh

$keyVaultName = 'rofl-ninja-secrets'
$secretName = 'sammo-oeis-api--config'

Install-Module -Name Az.KeyVault -Scope CurrentUser

Connect-AzAccount

$conf = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -AsPlainText

$conf | ConvertFrom-Json | ConvertTo-Json > "$PSScriptRoot/$secretName.json"