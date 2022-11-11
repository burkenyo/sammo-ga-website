#!/usr/bin/env pwsh

dotnet tool restore

dotnet test --collect:"XPlat Code Coverage" | Tee-Object -Variable lines

if ($LastExitCode)
{
    exit
}

$attachments = @()

$attachmentsFound = $false
foreach($line in $lines) {
    if ($attachmentsFound)  {
        $attachments += $line.Trim()
    }
    elseif($line -eq "Attachments:")
    {
        $attachmentsFound = $true
    }
}

foreach($testResult in $attachments) {
    $testResultDir = Split-Path $testResult
    $reportDir = Join-Path $testResultDir report

    dotnet reportgenerator -reports:$testResult -targetdir:$reportDir -reporttypes:html

    Invoke-Item (Join-Path $reportDir index.html)
}