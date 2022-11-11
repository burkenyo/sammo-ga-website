#!/usr/bin/env pwsh

foreach($testDir in Get-ChildItem TestResults -Recurse -Directory)
{
    $latest = Get-ChildItem $testDir/*/report | Sort-Object LastWriteTime | Select-Object -Last 1

    Invoke-Item $latest/index.html
}